using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TencentCloudCls
{
    public class Client
    {
        private const string UserAgent = "cls-dotnet-sdk-1.0.0";

        private readonly ClientProfile _cpf;
        private readonly ConcurrentDictionary<string, LogGroupEntry> _lgs = new();

        private readonly string _contextId = ClsHelper.CreateContextId();
        private long _lgId;
        private readonly AsyncQueue<UploadTask> _toUpload;

        private HttpClient _httpClient = new();
        private DateTime _httpClientCreateTime;

        private struct UploadTask
        {
            internal string TopicId;
            internal LogGroup LogGroup;
        }

        private class LogGroupEntry
        {
            internal string TopicId;
            internal LogGroup LogGroup;
            internal SendPolicy.Stat PolicyStat;
        }

        public Client(ClientProfile cpf)
        {
            cpf.Validate();
            _cpf = cpf;
            _toUpload = new AsyncQueue<UploadTask>((int)_cpf.SendPolicy.Worker);

            GetHttpClient();

            if (cpf.SendPolicy.EnableBatch)
            {
                for (var i = 0; i < cpf.SendPolicy.Worker; i++)
                {
                    Task.Run(UploadWorker);
                }

                if (cpf.SendPolicy.FlushInterval != default)
                {
                    Task.Run(IntervalUploadWorker);
                }
            }
        }

        public void UploadLog(string topicId, Log log)
        {
            UploadLogAsync(topicId, log).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task UploadLogAsync(string topicId, Log log)
        {
            if (string.IsNullOrEmpty(topicId))
            {
                throw new ArgumentNullException(nameof(topicId));
            }

            if (_cpf.SendPolicy.EnableBatch)
            {
                await UploadLogInBatch(topicId, log);
            }
            else
            {
                await UploadLogImmediate(topicId, log);
            }
        }

        private async Task UploadLogInBatch(string topicId, Log log)
        {
            var lge = _lgs.GetOrAdd(topicId, _ => new LogGroupEntry
            {
                TopicId = topicId,
                LogGroup = new LogGroup(),
                PolicyStat = new SendPolicy.Stat { LastUpload = DateTime.Now },
            });

            var logSize = log.CalculateSize();

            lock (lge)
            {
                InitializeLogGroup(lge.LogGroup);
                lge.LogGroup.Logs.Add(log);

                lge.PolicyStat.BatchCount++;
                lge.PolicyStat.BatchSize += (ulong)logSize;
            }

            await HintUpload(lge);
        }

        private async Task UploadLogImmediate(string topicId, Log log)
        {
            var lg = new LogGroup();
            InitializeLogGroup(lg);
            lg.Logs.Add(log);
            await FlushLogGroupEntry(topicId, lg);
        }

        private async Task HintUpload(LogGroupEntry lge)
        {
            LogGroup toUpload;

            lock (lge)
            {
                if (lge.LogGroup.Logs.Count == 0)
                {
                    return;
                }

                var countOk = lge.PolicyStat.BatchCount > _cpf.SendPolicy.MaxBatchCount;
                var sizeOk = lge.PolicyStat.BatchSize > _cpf.SendPolicy.MaxBatchSize;
                var intervalOk = _cpf.SendPolicy.FlushInterval != TimeSpan.Zero &&
                                 DateTime.Now - lge.PolicyStat.LastUpload > _cpf.SendPolicy.FlushInterval;
                var shouldUpload = countOk || sizeOk || intervalOk;

                if (!shouldUpload)
                {
                    return;
                }

                _cpf.Logger.Log(LogLevel.Debug,
                    $"HintUpload: " +
                    $"count={lge.LogGroup.Logs.Count} " +
                    $"last_upload={lge.PolicyStat.LastUpload} " +
                    $"size={lge.PolicyStat.BatchSize} ");

                toUpload = lge.LogGroup;
                lge.LogGroup = new LogGroup();
                InitializeLogGroup(lge.LogGroup);
                lge.PolicyStat.BatchCount = 0;
                lge.PolicyStat.BatchSize = 0;
                lge.PolicyStat.LastUpload = DateTime.Now;
            }

            await _toUpload.EnqueueAsync(new UploadTask
            {
                TopicId = lge.TopicId,
                LogGroup = toUpload,
            });
        }

        private void InitializeLogGroup(LogGroup lg)
        {
            if (!string.IsNullOrEmpty(_cpf.Source))
            {
                lg.Source = _cpf.Source;
            }

            if (!string.IsNullOrEmpty(_cpf.Hostname))
            {
                lg.Hostname = _cpf.Hostname;
            }

            lg.ContextFlow = ClsHelper.CreateContextFlow(_contextId, Interlocked.Increment(ref _lgId));
        }

        private HttpRequestMessage CreateRequest(string topicId, LogGroup lg)
        {
            var lgl = new LogGroupList();
            lgl.LogGroupList_.Add(lg);

            var uri = new Uri(
                $"{_cpf.Scheme}{_cpf.Endpoint}/structuredlog?topic_id={Uri.EscapeDataString(topicId)}");
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Add("User-Agent", UserAgent);

            _cpf.Compressor.CompressContent(request, lgl);

            var token = _cpf.Credential.Token;
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("X-Cls-Token", token);
            }

            var ak = _cpf.Credential.SecretId;
            var sk = _cpf.Credential.SecretKey;
            if (string.IsNullOrWhiteSpace(ak) || string.IsNullOrWhiteSpace(sk))
            {
                throw new ArgumentException("SecretId/SecretKey is empty");
            }

            request.Headers.TryAddWithoutValidation("Authorization", ClsHelper.GetAuthorization(
                request, topicId, ak, sk));

            return request;
        }

        private async Task FlushLogGroupEntry(string topicId, LogGroup lg)
        {
            var maxDelay = _cpf.SendPolicy.MaxRetryInterval;
            var delay = TimeSpan.FromSeconds(1);

            for (var i = 0; i <= _cpf.SendPolicy.MaxRetry; i++)
            {
                using var req = CreateRequest(topicId, lg);
                _cpf.Logger.Log(LogLevel.Debug, $"FlushLogGroupEntry.Start: topic={topicId} logs={lg.Logs.Count}");
                using var resp = await GetHttpClient().SendAsync(req);
                _cpf.Logger.Log(LogLevel.Debug, $"FlushLogGroupEntry.End: topic={topicId} logs={lg.Logs.Count}");
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }

                _cpf.Logger.Log(LogLevel.Debug, $"FlushLogGroupEntry.Retry: topic={topicId} logs={lg.Logs.Count} retry={i}");
                if (i < _cpf.SendPolicy.MaxRetry && Retryable(resp))
                {
                    await Task.Delay(delay);
                    delay += delay;
                    if (delay > maxDelay)
                    {
                        delay = maxDelay;
                    }
                    continue;
                }

                _cpf.Logger.Log(LogLevel.Debug, $"FlushLogGroupEntry.Throw: topic={topicId} logs={lg.Logs.Count}");

                var requestId = resp.Headers.GetValues("X-Cls-Requestid").FirstOrDefault();
                var httpBody = await resp.Content.ReadAsStringAsync();
                throw new TencentCloudSdkError(resp.StatusCode, requestId, httpBody);
            }
        }

        private static bool Retryable(HttpResponseMessage response)
        {
            return response.StatusCode != HttpStatusCode.Unauthorized &&
                   response.StatusCode != HttpStatusCode.Forbidden &&
                   response.StatusCode != HttpStatusCode.NotFound &&
                   response.StatusCode != HttpStatusCode.RequestEntityTooLarge;
        }

        private async void UploadWorker()
        {
            while (true)
            {
                var task = await _toUpload.DequeueAsync();
                try
                {
                    await FlushLogGroupEntry(task.TopicId, task.LogGroup);
                }
                catch (TencentCloudSdkError e)
                {
                    _cpf.Logger.Log(LogLevel.Error,
                        $"UploadWorker.Upload: topic={task.TopicId} logs={task.LogGroup.Logs.Count} err={e}");
                }
            }
        }

        private async void IntervalUploadWorker()
        {
            while (true)
            {
                await Task.Delay(_cpf.SendPolicy.FlushInterval);
                foreach (var lge in _lgs.Values)
                {
                    await HintUpload(lge);
                }
            }
        }

        private HttpClient GetHttpClient()
        {
            // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
            var pooledClientLifetime = TimeSpan.FromMinutes(15);
            if (DateTime.Now - _httpClientCreateTime > pooledClientLifetime)
            {
                lock (this)
                {
                    _httpClient = new HttpClient();
                    _httpClientCreateTime = DateTime.Now;
                }
            }

            return _httpClient;
        }
    }
}
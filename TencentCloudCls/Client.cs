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

            ValidateLog(log);

            if (_cpf.SendPolicy.EnableBatch)
            {
                await UploadLogInBatch(topicId, log);
            }
            else
            {
                await UploadLogImmediate(topicId, log);
            }
        }

        private void ValidateLog(Log log)
        {
            const int kvSizeLimit = 1 * 1024 * 1024;
            const int logSizeLimit = 5 * 1024 * 1024;

            var logSize = 0;
            foreach (var logContent in log.Contents)
            {
                logSize += logContent.Key.Length;
                logSize += logContent.Value.Length;
                if (logContent.Key.Length > kvSizeLimit || logContent.Value.Length > kvSizeLimit)
                {
                    throw new TencentCloudSdkError("The size of the log key/value cannot exceed 1MB");
                }
            }

            if (logSize > logSizeLimit)
            {
                throw new TencentCloudSdkError("The size of the log cannot exceed 5MB");
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

                var countOk = lge.PolicyStat.BatchCount >= _cpf.SendPolicy.MaxBatchCount;
                var sizeOk = lge.PolicyStat.BatchSize >= _cpf.SendPolicy.MaxBatchSize;
                var intervalOk = _cpf.SendPolicy.FlushInterval != TimeSpan.Zero &&
                                 DateTime.Now - lge.PolicyStat.LastUpload >= _cpf.SendPolicy.FlushInterval;
                var shouldUpload = countOk || sizeOk || intervalOk;

                if (!shouldUpload)
                {
                    return;
                }

                _cpf.Logger.Log(LogLevel.Debug, "HintUpload: count={} last_upload={} size={}", lge.LogGroup.Logs.Count,
                    lge.PolicyStat.LastUpload, lge.PolicyStat.BatchSize);

                toUpload = lge.LogGroup;
                lge.LogGroup = new LogGroup();
                InitializeLogGroup(lge.LogGroup);
                lge.PolicyStat.BatchCount = 0;
                lge.PolicyStat.BatchSize = 0;
                lge.PolicyStat.LastUpload = DateTime.Now;
            }

            var enqueued = await _toUpload.EnqueueAsync(new UploadTask
            {
                TopicId = lge.TopicId,
                LogGroup = toUpload,
            }, _cpf.SendPolicy.EnqueueTimeout);
            if (!enqueued)
            {
                _cpf.Logger.LogWarning("HintUpload.Discard: topic_id={} count={}", lge.TopicId,
                    lge.LogGroup.Logs.Count);
            }
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
                try
                {
                    using var req = CreateRequest(topicId, lg);
                    _cpf.Logger.Log(LogLevel.Debug, "FlushLogGroupEntry.Start: topic={} logs={}", topicId,
                        lg.Logs.Count);

                    using var resp = await GetHttpClient().SendAsync(req);
                    _cpf.Logger.Log(LogLevel.Debug, "FlushLogGroupEntry.End: topic={} logs={}", topicId, lg.Logs.Count);
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        return;
                    }

                    if (!await ShouldRetryAndWait(i, resp))
                    {
                        _cpf.Logger.Log(LogLevel.Error, "FlushLogGroupEntry.Throw: topic={} logs={}", topicId,
                            lg.Logs.Count);
                        var requestId = resp.Headers.GetValues("X-Cls-Requestid").FirstOrDefault();
                        var httpBody = await resp.Content.ReadAsStringAsync();
                        throw new TencentCloudSdkError(resp.StatusCode, requestId, httpBody);
                    }

                    _cpf.Logger.Log(LogLevel.Warning, "FlushLogGroupEntry.Retry: topic={} logs={} retry={}", topicId,
                        lg.Logs.Count, i);
                }
                catch (Exception e)
                {
                    _cpf.Logger.Log(LogLevel.Error, "FlushLogGroupEntry.Err: topic={} logs={} err={}", topicId,
                        lg.Logs.Count, e);

                    if (!await ShouldRetryAndWait(i, null))
                    {
                        throw;
                    }

                    _cpf.Logger.Log(LogLevel.Warning, "FlushLogGroupEntry.Retry: topic={} logs={} retry={}", topicId,
                        lg.Logs.Count, i);
                }
            }

            async Task<bool> ShouldRetryAndWait(int retryTimes, HttpResponseMessage resp)
            {
                if (retryTimes >= _cpf.SendPolicy.MaxRetry)
                    return false;

                if (resp != null && !IsRetryableResp(resp))
                    return false;

                await Task.Delay(delay);

                delay += delay;
                if (delay > maxDelay)
                {
                    delay = maxDelay;
                }

                return true;
            }
        }

        private static bool IsRetryableResp(HttpResponseMessage response)
        {
            return response.StatusCode != HttpStatusCode.Unauthorized &&
                   response.StatusCode != HttpStatusCode.Forbidden &&
                   response.StatusCode != HttpStatusCode.NotFound &&
                   response.StatusCode != HttpStatusCode.RequestEntityTooLarge;
        }

        private async Task UploadWorker()
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
                    _cpf.Logger.Log(LogLevel.Error, "UploadWorker.Upload: topic={} logs={} err={}", task.TopicId,
                        task.LogGroup.Logs.Count, e);
                }
                catch (Exception e)
                {
                    _cpf.Logger.Log(LogLevel.Error, "UploadWorker.FlushLogGroupEntry.Error: err={}", e);
                }
            }
        }

        private async Task IntervalUploadWorker()
        {
            while (true)
            {
                await Task.Delay(_cpf.SendPolicy.FlushInterval);
                foreach (var lge in _lgs.Values)
                {
                    try
                    {
                        await HintUpload(lge);
                    }
                    catch (Exception e)
                    {
                        _cpf.Logger.Log(LogLevel.Error, "IntervalUploadWorker.HintUpload.Error: err={}", e);
                    }
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
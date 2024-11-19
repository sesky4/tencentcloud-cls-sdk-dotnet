using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using Google.Protobuf;

namespace TencentCloudCls
{
    public class Client
    {
        private const string UserAgent = "cls-dotnet-sdk-1.0.0";

        private readonly ClientProfile _cpf;
        private readonly HttpClient _httpClient;
        private ConcurrentDictionary<string, LogGroup> _lgs;
        private int _batchId;

        private class LogGroupContainer
        {
            public LogGroup LogGroup;
        }

        public Client(ClientProfile cpf)
        {
            cpf.Validate();

            _cpf = cpf;
            _httpClient = new HttpClient();
        }

        public void UploadLog(string topicId, Log log)
        {
            var lg = _lgs.GetOrAdd(topicId, s => new LogGroup
            {
                ContextFlow = ClsHelper.ProducerHash,
                Source = _cpf.Source,
                Hostname = _cpf.Hostname,
            });

            lock (lg)
            {
                lg.Logs.Add(log);
            }

            HintUpload();
        }

        private void HintUpload()
        {
        }

        public async void UploadLogAsync()
        {
        }

        private void Flush()
        {
            foreach (var kv in _lgs)
            {
                FlushLogs(kv.Value);
            }
        }

        private async void Loop()
        {
        }

        private HttpRequestMessage MakeRequest(LogGroup lg)
        {
            var lgl = new LogGroupList();
            lgl.LogGroupList_.Add(lg);
            var body = lgl.ToByteArray();

            var req = new HttpRequestMessage();
            req.Content = new ByteArrayContent(body);
            req.Headers.Add("Content-Type", "application/x-protobuf");
            req.Headers.Add("User-Agent", UserAgent);

            var token = _cpf.Credential.Token;
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Add("X-Cls-Token", token);
            }

            req.Headers.Authorization = new AuthenticationHeaderValue("");
            req.Headers.Add("Authorization",
                ClsHelper.SignRequest(req, _cpf.Credential.SecretId, _cpf.Credential.SecretKey));

            return req;
        }

        private async void FlushLogs(LogGroup lg)
        {
            using var req = MakeRequest(lg);
            var resp = await _httpClient.SendAsync(req);
        }
    }
}
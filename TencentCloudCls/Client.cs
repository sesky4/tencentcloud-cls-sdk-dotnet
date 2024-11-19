using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Google.Protobuf;

namespace TencentCloudCls
{
    public class Client
    {
        private const string UserAgent = "cls-dotnet-sdk-1.0.0";

        private readonly ClientProfile _cpf;
        private readonly HttpClient _httpClient;
        private ConcurrentDictionary<string, LogGroupEntry> _lgs;
        private int _batchId;

        private class LogGroupEntry
        {
            public string TopicId;
            public LogGroup LogGroup;
        }

        public Client(ClientProfile cpf)
        {
            cpf.Validate();

            _cpf = cpf;
            _httpClient = new HttpClient();
            _lgs = new ConcurrentDictionary<string, LogGroupEntry>();
        }

        public void UploadLog(string topicId, Log log)
        {
            var lge = _lgs.GetOrAdd(topicId, _ => new LogGroupEntry
            {
                TopicId = topicId,
                LogGroup = new LogGroup
                {
                    ContextFlow = ClsHelper.ProducerHash,
                    Source = _cpf.Source,
                    Hostname = _cpf.Hostname,
                },
            });

            lock (lge)
            {
                lge.LogGroup.Logs.Add(log);
            }

            HintUpload();
        }

        private void HintUpload()
        {
            Flush();
        }

        public async void UploadLogAsync()
        {
        }

        private void Flush()
        {
            foreach (var lge in _lgs.Values)
            {
                FlushLogGroupEntry(lge).GetAwaiter().GetResult();
            }
        }

        private async void Loop()
        {
        }

        private HttpRequestMessage MakeRequest(LogGroupEntry lge)
        {
            var lgl = new LogGroupList();
            lgl.LogGroupList_.Add(lge.LogGroup);
            var body = lgl.ToByteArray();

            var uri = $"{_cpf.Scheme}{_cpf.Endpoint}/structuredlog";
            var req = new HttpRequestMessage(HttpMethod.Post, uri);
            req.Content = new ByteArrayContent(body);
            req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-protobuf");
            req.Headers.Add("User-Agent", UserAgent);

            var token = _cpf.Credential.Token;
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Add("X-Cls-Token", token);
            }

            req.Headers.Authorization = new AuthenticationHeaderValue("", ClsHelper.GetAuthorization(
            req, lge.TopicId, _cpf.Credential.SecretId, _cpf.Credential.SecretKey));
            req.Headers.Add("Authorization", ClsHelper.GetAuthorization(
                req, lge.TopicId, _cpf.Credential.SecretId, _cpf.Credential.SecretKey));

            return req;
        }

        private async Task FlushLogGroupEntry(LogGroupEntry lge)
        {
            using var req = MakeRequest(lge);
            var resp = await _httpClient.SendAsync(req);
        }
    }
}
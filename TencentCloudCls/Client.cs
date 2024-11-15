using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace TencentCloudCls
{
    public class Client
    {
        private const string SdkVersion = "cls-dotnet-sdk-1.0.0";

        private readonly ClientProfile _cpf;
        private readonly HttpClient _httpClient;

        public Client(ClientProfile cpf)
        {
            _cpf = cpf;
            _httpClient = new HttpClient();
        }

        public void UploadLog(Log log)
        {
        }

        public async void UploadLogAsync()
        {
        }

        private void Flush()
        {
        }

        private async void Loop()
        {
            while (true)
            {
                Task.WhenAny(Task.Delay(30), Task.FromResult());
            }
        }

        private HttpRequestMessage BuildRequest()
        {
            var lgl = new LogGroupList();
            lgl.ToByteArray();
            lock (this)
            {
            }

            var req = new HttpRequestMessage();
            req.Content = new StreamContent();
            req.Headers.Add("Content-Type", "application/x-protobuf");
            req.Headers.Add("User-Agent", SdkVersion);

            var token = _cpf.Credential.Token;
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Add("X-Cls-Token", token);
            }

            req.Headers.Add("Authorization",
                ClsHelper.SignRequest(req, _cpf.Credential.SecretId, _cpf.Credential.SecretKey));

            return req;
        }

        private async void UploadLogs()
        {
            using var req = BuildRequest();
            var resp = await _httpClient.SendAsync(req);
        }
    }
}
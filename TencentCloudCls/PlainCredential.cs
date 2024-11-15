namespace TencentCloudCls
{
    public class PlainCredential : ICredential
    {
        public string SecretId { get; }
        public string SecretKey { get; }
        public string Token { get; }

        public PlainCredential(string secretId, string secretKey, string token = "")
        {
            SecretId = secretId;
            SecretKey = secretKey;
            Token = token;
        }
    }
}
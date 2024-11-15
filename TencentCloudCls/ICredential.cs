namespace TencentCloudCls
{
    public interface ICredential
    {
        string SecretId { get; }
        string SecretKey { get; }
        string Token { get; }
    }
}
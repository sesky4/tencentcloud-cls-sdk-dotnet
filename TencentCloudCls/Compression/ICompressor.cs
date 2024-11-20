using System.Net.Http;
using Google.Protobuf;

namespace TencentCloudCls.Compression
{
    public interface ICompressor
    {
        public void CreateContent(HttpRequestMessage request, IMessage message);
    }
}
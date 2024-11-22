using System.Net.Http;
using Google.Protobuf;

namespace TencentCloudCls.Compression
{
    public interface ICompressor
    {
        internal void CompressContent(HttpRequestMessage request, IMessage message);
    }
}
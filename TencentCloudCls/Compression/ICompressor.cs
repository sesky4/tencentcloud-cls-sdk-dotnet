using System.Net.Http;
using Google.Protobuf;

namespace TencentCloudCls.Compression
{
    public interface ICompressor
    {
        internal HttpContent Compress(IMessage message);
    }
}
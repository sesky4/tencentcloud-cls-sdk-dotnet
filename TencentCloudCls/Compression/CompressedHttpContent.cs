using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TencentCloudCls.Compression
{
    public class CompressedHttpContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw new System.NotImplementedException();
        }

        protected override bool TryComputeLength(out long length)
        {
            throw new System.NotImplementedException();
        }
    }
}
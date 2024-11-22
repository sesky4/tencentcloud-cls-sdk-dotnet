using System.Net.Http;
using System.Net.Http.Headers;
using Google.Protobuf;

namespace TencentCloudCls.Compression
{
    public class NoCompressor : ICompressor
    {
        void ICompressor.CompressContent(HttpRequestMessage request, IMessage message)
        {
            var body = message.ToByteArray();
            var httpContent = new ByteArrayContent(body);
            httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-protobuf");
            request.Content = httpContent;
        }
    }
}
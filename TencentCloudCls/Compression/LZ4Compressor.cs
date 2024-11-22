using System.Net.Http;
using System.Net.Http.Headers;
using Google.Protobuf;
using K4os.Compression.LZ4;

namespace TencentCloudCls.Compression
{
    public class Lz4Compressor : ICompressor
    {
        public LZ4Level Lz4Level = LZ4Level.L00_FAST;

        void ICompressor.CompressContent(HttpRequestMessage request, IMessage message)
        {
            var body = message.ToByteArray();
            var lz4Body = new byte[LZ4Codec.MaximumOutputSize(body.Length)];
            var len = LZ4Codec.Encode(body, lz4Body, Lz4Level);
            var httpContent = new ByteArrayContent(lz4Body, 0, len);
            httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-protobuf");
            request.Content = httpContent;
            request.Headers.Add("X-Cls-Compress-Type", "lz4");
        }
    }
}
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace TencentCloudCls.Compression
{
    internal class MyContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw new NotImplementedException();
        }

        protected override bool TryComputeLength(out long length)
        {
            throw new NotImplementedException();
        }
    }

    internal class MyStream : Stream
    {
        internal IMessage Message;

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get; }
        public override long Position { get; set; }
    }

    public class NoCompressor : ICompressor
    {
        HttpContent ICompressor.Compress(IMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
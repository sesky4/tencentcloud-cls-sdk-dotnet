using System;
using TencentCloudCls.Compression;

namespace TencentCloudCls
{
    public class ClientProfile
    {
        public ICredential Credential;
        public ICompressor Compressor = new NoCompressor();

        public string Scheme = "https://";
        public string Endpoint;
        public string Source;
        public string Hostname;

        internal void Validate()
        {
            if (Credential == null)
            {
                throw new ArgumentException("Credential can not be null");
            }

            if (Compressor == null)
            {
                throw new ArgumentException("Compressor can not be null");
            }

            if (string.IsNullOrEmpty(Scheme))
            {
                throw new ArgumentException("Scheme can not be empty");
            }

            if (Scheme != "http://" && Scheme != "https://")
            {
                throw new ArgumentException($"Scheme not valid: {Scheme}");
            }

            if (string.IsNullOrEmpty(Endpoint))
            {
                throw new ArgumentException("Endpoint can not be empty");
            }
        }
    }
}
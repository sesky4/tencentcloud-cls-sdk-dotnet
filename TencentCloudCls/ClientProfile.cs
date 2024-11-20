using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TencentCloudCls.Compression;

namespace TencentCloudCls
{
    public class ClientProfile
    {
        public ICredential Credential;
        public ICompressor Compressor = new Lz4Compressor();
        public SendPolicy SendPolicy = SendPolicy.Default;

        public string Scheme = "https://";
        public string Endpoint;
        public string Source = "";
        public string Hostname = "";
        public ILogger Logger = NullLogger.Instance;

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

            if (Source == null)
            {
                throw new ArgumentException("Source can not be empty");
            }

            if (Hostname == null)
            {
                throw new ArgumentException("Hostname can not be empty");
            }

            SendPolicy.Validate();
        }
    }
}
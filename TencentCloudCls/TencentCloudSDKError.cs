using System;
using System.Net;

namespace TencentCloudCls
{
    public class TencentCloudSdkError : Exception
    {
        public readonly HttpStatusCode HttpStatusCode;
        public readonly string RequestId;
        public readonly string Reason;

        public TencentCloudSdkError(HttpStatusCode httpStatusCode, string requestId, string reason)
        {
            HttpStatusCode = httpStatusCode;
            RequestId = requestId;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"Status={HttpStatusCode} RequestId={RequestId} Reason={Reason}";
        }
    }
}
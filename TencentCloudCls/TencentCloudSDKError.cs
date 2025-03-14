using System;
using System.Net;

namespace TencentCloudCls
{
    public class TencentCloudSdkError : Exception
    {
        public readonly HttpStatusCode HttpStatusCode;
        public readonly string RequestId;
        public readonly string Reason;

        public TencentCloudSdkError(string reason) : this(HttpStatusCode.OK, "", reason)
        {
        }

        public TencentCloudSdkError(HttpStatusCode httpStatusCode, string requestId, string reason)
            : base(FormatError(httpStatusCode, requestId, reason))
        {
            HttpStatusCode = httpStatusCode;
            RequestId = requestId;
            Reason = reason;
        }

        public override string ToString()
        {
            return FormatError(HttpStatusCode, RequestId, Reason);
        }

        private static string FormatError(HttpStatusCode httpStatusCode, string requestId, string reason)
        {
            return $"Status={httpStatusCode} RequestId={requestId} Reason={reason}";
        }
    }
}
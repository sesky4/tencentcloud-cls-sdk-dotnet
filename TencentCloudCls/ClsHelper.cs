using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace TencentCloudCls
{
    public static class ClsHelper
    {
        public static Log MakeLogGroup(long time, Dictionary<string, string> logEntries)
        {
            var log = new Log
            {
                Time = time
            };

            log.Contents.AddRange(logEntries.Select(e => new Log.Types.Content
            {
                Key = e.Key,
                Value = e.Value,
            }));

            return log;
        }

        // https://cloud.tencent.com/document/product/614/12445
        public static string GetAuthorization(
            HttpRequestMessage request, string topicId, string secretId, string secretKey)
        {
            var formattedParams = $"topic_id={topicId}";

            var formattedHeaders =
                $"content-type={Uri.EscapeDataString(request.Content.Headers.ContentType.ToString())}" +
                $"&host={Uri.EscapeDataString(request.RequestUri.Host)}";

            var httpRequestInfo =
                $"{request.Method.Method.ToLower()}\n" +
                $"{request.RequestUri.AbsolutePath}\n" +
                $"{formattedParams}\n" +
                $"{formattedHeaders}\n";

            using var sha1 = SHA1.Create();
            var hriSha1 = sha1.ComputeHash(Encoding.UTF8.GetBytes(httpRequestInfo));
            var hriSha1Hex = BitConverter.ToString(hriSha1).Replace("-", "").ToLower();
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            var signTime = $"{now - 60};{now + 300}";
            var string2Sign = $"sha1\n{signTime}\n{hriSha1Hex}\n";

            using var hmacSha1 = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey));
            var signKeyHmac = hmacSha1.ComputeHash(Encoding.UTF8.GetBytes(signTime));
            var signKeyHmacHex = BitConverter.ToString(signKeyHmac).Replace("-", "").ToLower();

            hmacSha1.Initialize();
            hmacSha1.Key = Encoding.UTF8.GetBytes(signKeyHmacHex);
            var sign = hmacSha1.ComputeHash(Encoding.UTF8.GetBytes(string2Sign));
            var signHex = BitConverter.ToString(sign).Replace("-", "").ToLower();

            return $"q-sign-algorithm=sha1" +
                   $"&q-ak={secretId}" +
                   $"&q-sign-time={signTime}" +
                   $"&q-key-time={signTime}" +
                   $"&q-header-list=content-type;host" +
                   $"&q-url-param-list=topic_id" +
                   $"&q-signature={signHex}";
        }


        // todo:
        public static string ProducerHash => "";
    }
}
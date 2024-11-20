using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace TencentCloudCls
{
    public static class ClsHelper
    {
        public static Log CreateLogGroup(Dictionary<string, string> logEntries)
        {
            var log = new Log
            {
                Time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
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

        internal static string CreateContextId()
        {
            return Random.Shared.NextInt64().ToString("X");
        }

        internal static string CreateContextFlow(string contextId, ulong logGroupId)
        {
            return $"{contextId}-{logGroupId:X}";
        }

        public static string GetIpAddress()
        {
            foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                {
                    continue;
                }

                return address.ToString();
            }

            throw new Exception("no valid ip address found");
        }

        public static string GetHostname()
        {
            return Dns.GetHostName();
        }
    }
}
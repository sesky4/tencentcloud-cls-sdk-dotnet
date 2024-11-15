using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace TencentCloudCls
{
    public static class ClsHelper
    {
        public static Log LogFromDictionary(long time, Dictionary<string, string> logEntries)
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

        public static string SignRequest(HttpRequestMessage request, string secretId, string secretKey)
        {
        }
    }
}
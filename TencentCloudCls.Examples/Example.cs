using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using TencentCloudCls.Compression;

namespace TencentCloudCls.Examples
{
    public static class Example
    {
        public static void Main()
        {
            var scheme = "https://";
            var endpoint = "ap-guangzhou.cls.tencentcs.com";
            var secretId = Environment.GetEnvironmentVariable("TENCENTCLOUD_SECRET_ID")!;
            var secretKey = Environment.GetEnvironmentVariable("TENCENTCLOUD_SECRET_KEY")!;
            // 临时密钥需要 token, 如果非临时密钥 token 赋值为空即可
            var token = Environment.GetEnvironmentVariable("TENCENTCLOUD_TOKEN")!;
            var topicId = Environment.GetEnvironmentVariable("CLS_TOPIC_ID")!;

            var cpf = new ClientProfile
            {
                Scheme = scheme,
                Endpoint = endpoint,
                SendPolicy = SendPolicy.SmallBatch,
                Compressor = new Lz4Compressor(),
                Credential = new PlainCredential(secretId, secretKey, token),
                Source = ClsHelper.GetIpAddress(),
                Hostname = ClsHelper.GetHostname(),
                Logger = LoggerFactory.Create(
                    builder =>
                    {
                        builder.AddSimpleConsole(c =>
                        {
                            c.SingleLine = true;
                            c.TimestampFormat = "[HH:mm:ss.fff] ";
                        });
                        builder.SetMinimumLevel(LogLevel.Debug);
                    }
                ).CreateLogger(""),
            };

            var client = new Client(cpf);

            for (var i = 0; i < 3; i++)
            {
                var threadNo = i;
                new Thread(() =>
                {
                    for (var j = 0ul; j < 100; j++)
                    {
                        client.UploadLog(topicId, ClsHelper.CreateLogGroup(new Dictionary<string, string>
                                {
                                    { "t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") },
                                    { "num", j.ToString() },
                                    { "msg", string.Concat(Enumerable.Repeat(Guid.NewGuid().ToString(), 20)) },
                                }
                            )
                        );
                        Console.WriteLine($"upload: thread={threadNo} index={j}");
                    }
                }).Start();
            }


            Console.Read();
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TencentCloudCls.Compression;

namespace TencentCloudCls.Examples
{
    public class Example
    {
        public static void Main()
        {
            var scheme = "https://";
            var endpoint = "ap-guangzhou.cls.tencentcs.com";
            var secretId = Environment.GetEnvironmentVariable("TENCENTCLOUD_SECRET_ID")!;
            var secretKey = Environment.GetEnvironmentVariable("TENCENTCLOUD_SECRET_KEY")!;
            var topicId = Environment.GetEnvironmentVariable("CLS_TOPIC_ID")!;
            
            var cpf = new ClientProfile
            {
                Scheme = scheme,
                Endpoint = endpoint,
                SendPolicy = SendPolicy.SmallBatch,
                Compressor = new Lz4Compressor(),
                Credential = new PlainCredential(secretId, secretKey),
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

            for (var i = 0ul; i < ulong.MaxValue; i++)
            {
                client.UploadLog(topicId, ClsHelper.CreateLogGroup(new Dictionary<string, string>
                        {
                            { "t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") },
                            { "num", i.ToString() },
                        }
                    )
                );
            }

            Console.Read();
        }
    }
}
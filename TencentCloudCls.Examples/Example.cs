using System;
using System.Collections.Generic;

namespace TencentCloudCls.Examples
{
    public class Example
    {
        public static void Main()
        {
            var cpf = new ClientProfile
            {
                Scheme = "http://",
                // Endpoint = "ap-guangzhou.cls.tencentcs.com",
                Endpoint = "127.0.0.1",
                Credential = new PlainCredential(
                    "", ""),
            };
            var client = new Client(cpf);

            client.UploadLog("98871791-0320-47a5-ac73-9075b00989bc",
                ClsHelper.MakeLogGroup(
                    DateTimeOffset.Now.ToUnixTimeSeconds(),
                    new Dictionary<string, string>
                    {
                        { "key1", "val1" },
                        { "key2", "val2" },
                    }
                )
            );
        }
    }
}
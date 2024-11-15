using System.Collections.Generic;

namespace TencentCloudCls.Examples
{
    public class Example
    {
        public static void Main()
        {
            var cpf = new ClientProfile
            {
                Endpoint = null,
                Credential = new PlainCredential("", ""),
            };
            var client = new Client(cpf);

            client.UploadLog(ClsHelper.LogFromDictionary(123, new Dictionary<string, string>
            {
            }));
        }
    }
}
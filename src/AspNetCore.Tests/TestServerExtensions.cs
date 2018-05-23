using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore
{
    public static class TestServerExtensions
    {
        public static Task<HttpResponseMessage> SendRequestAsync<TObject>(this TestServer testServer, TObject requestBody)
        {
            return SendRequestAsync(testServer, JsonConvert.SerializeObject(requestBody));
        }

        public static Task<HttpResponseMessage> SendRequestAsync(this TestServer testServer, string requestBody)
        {
            return testServer.CreateClient()
                .PostAsync("http://localhost:5000",
                    new StringContent(requestBody,
                    Encoding.UTF8, "application/json"));
        }
    }
}

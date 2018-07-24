using System;
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
            return SendPostRequestAsync(testServer, JsonConvert.SerializeObject(requestBody));
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(this TestServer testServer, string requestBody)
        {
            return testServer.CreateClient()
                .PostAsync("http://localhost:5000",
                    new StringContent(requestBody,
                    Encoding.UTF8, "application/json"));
        }

        public static Task<HttpResponseMessage> SendGetRequestAsync(this TestServer testServer, string query)
        {
            string normalizedQuery = query
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            return testServer.CreateClient()
                .GetAsync($"http://localhost:5000?query={normalizedQuery}");
        }

        public static Task<HttpResponseMessage> SendGetRequestAsync(this TestServer testServer, Uri requestUri)
        {
            return testServer.CreateClient()
                .GetAsync(requestUri);
        }
    }
}

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Newtonsoft.Json;

namespace HotChocolate.AspNetClassic
{
    public static class TestServerExtensions
    {
        public static Task<HttpResponseMessage> SendPostRequestAsync<TObject>(
            this TestServer testServer,
            TObject requestBody,
            string path = null)
        {
            return SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(requestBody),
                path?.TrimStart('/'));
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer,
            string requestBody,
            string path = null)
        {
            return SendPostRequestAsync(
                testServer,
                requestBody,
                "application/json",
                path?.TrimStart('/'));
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, string requestBody,
            string contentType, string path)
        {
            return testServer.HttpClient
                .PostAsync(CreateUrl(path),
                    new StringContent(requestBody,
                        Encoding.UTF8, contentType));
        }

        public static Task<HttpResponseMessage> SendGetRequestAsync(
            this TestServer testServer,
            string query,
            string path = null)
        {
            string normalizedQuery = query
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            return testServer.HttpClient
                .GetAsync($"{CreateUrl(path)}?query={normalizedQuery}");
        }

        public static string CreateUrl(string path)
        {
            string url = "http://localhost:5000";
            if (path != null)
            {
                url += "/" + path.TrimStart('/');
            }
            return url;
        }
    }
}

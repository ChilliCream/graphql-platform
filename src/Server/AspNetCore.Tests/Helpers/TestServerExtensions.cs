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
        public static Task<HttpResponseMessage> SendRequestAsync<TObject>(
            this TestServer testServer, TObject requestBody, string path = null)
        {
            return SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(requestBody),
                path?.TrimStart('/'));
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, string requestBody, string path = null)
        {
            return SendPostRequestAsync(
                testServer, requestBody,
                "application/json", path);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, string requestBody,
            string contentType, string path)
        {
            var content = new StringContent(requestBody,
                Encoding.UTF8, contentType);

            return SendPostRequestAsync(testServer, content, path);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, HttpContent content, string path)
        {
            return testServer.CreateClient()
                .PostAsync(CreateUrl(path), content);
        }

        public static Task<HttpResponseMessage> SendGetRequestAsync(
            this TestServer testServer, string query, string path = null)
        {
            string normalizedQuery = query
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            return testServer.CreateClient()
                .GetAsync($"{CreateUrl(path)}?query={normalizedQuery}");
        }


        public static Task<HttpResponseMessage> SendMultipartRequestAsync<TObject>(
            this TestServer testServer, TObject requestBody, string path = null)
        {
            return SendPostMultipartRequestAsync(
                testServer,
                JsonConvert.SerializeObject(requestBody),
                path?.TrimStart('/'));
        }

        public static Task<HttpResponseMessage> SendPostMultipartRequestAsync(
            this TestServer testServer, string requestBody, string path = null)
        {
            var content = new MultipartContent("form-data")
            {
                new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            return SendPostRequestAsync(
                testServer, content, path);
        }

        private static string CreateUrl(string path)
        {
            string url = "http://localhost:5000";
            if (path != null)
            {
                url += "/" + path;
            }
            return url;
        }
    }
}

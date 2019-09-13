using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore.Tests.Utilities
{
    public static class TestServerExtensions
    {
        public static Task<HttpResponseMessage> SendPostRequestAsync<TObject>(
            this TestServer testServer, TObject requestBody, string path = null)
        {
            return SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(requestBody),
                path);
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

<<<<<<< HEAD:src/Server/AspNetCore.Tests/Helpers/TestServerExtensions.cs

        public static Task<HttpResponseMessage> SendMultipartRequestAsync<TObject>(
            this TestServer testServer,
            TObject requestBody,
            string path = null)
        {
            // dictionary key is variable name

            var boundary = Guid.NewGuid().ToString("N");
            var content = new MultipartFormDataContent(boundary)
            {
                { new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8, "application/json"),
                    "operations" }
            };

            Dictionary<string, ICollection<ClientQueryRequestFile>> files = null;
            if (requestBody is ClientQueryRequest queryRequest)
            {
                files = queryRequest.Files;
            }

            if (files?.Count > 0)
            {
                foreach (var variable in files)
                {
                    foreach (var v in variable.Value)
                    {
                        content.Add(new StreamContent(v.Stream), v.Name, v.FileName);
                    }
                }

                var map = new Dictionary<string, string[]>();
                var idx = 0;
                foreach (var item in files.GroupBy(x => x.Key))
                {
                    var variableName = $"variables.{item.Key}";

                    foreach (var valueGroup in item)
                    {
                        foreach (var value in valueGroup.Value)
                        {
                            var name = variableName;
                            if (item.Count() > 1)
                            {
                                name += $".{idx++}";
                            }
                            map.Add(value.Name, new []{ name });
                        }
                    }

                    idx = 0;
                }

                content.Add(new StringContent(JsonConvert.SerializeObject(map)), "map");
            }

            return SendPostRequestAsync(
                testServer,
                content,
                path?.TrimStart('/'));
        }

        private static string CreateUrl(string path)
=======
        public static string CreateUrl(string path)
>>>>>>> multipart_request:src/Server/AspNetCore.Tests.Utilities/TestServerExtensions.cs
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

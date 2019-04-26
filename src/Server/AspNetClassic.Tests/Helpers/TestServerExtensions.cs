using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Newtonsoft.Json;

namespace HotChocolate.AspNetClassic
{
    public static class TestServerExtensions
    {
        public static Task<HttpResponseMessage> SendRequestAsync<TObject>(
            this TestServer testServer,
            TObject requestBody,
            string path = null)
        {
            return SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(requestBody),
                path);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer,
            string requestBody,
            string path = null)
        {
            return SendPostRequestAsync(
                testServer,
                requestBody,
                "application/json", path);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, string requestBody,
            string contentType, string path)
        {
            return SendPostRequestAsync(
                    testServer,
                    new StringContent(requestBody,
                        Encoding.UTF8, contentType),
                    path);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, HttpContent content, string path)
        {
            return testServer.HttpClient
                .PostAsync(CreateUrl(path), content);
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

        public static Task<HttpResponseMessage> SendMultipartRequestAsync<TObject>(
            this TestServer testServer,
            TObject requestBody,
            string path = null)
        {
            // dictionary key is variable name

            var boundary = Guid.NewGuid().ToString("N");
            var content = new MultipartFormDataContent(boundary)
            {
                {
                    new StringContent(
                        JsonConvert.SerializeObject(requestBody),
                        Encoding.UTF8,
                        "application/json"),
                    "operations"
                }
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

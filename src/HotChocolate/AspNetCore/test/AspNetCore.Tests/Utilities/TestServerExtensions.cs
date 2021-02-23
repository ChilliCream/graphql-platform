using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore.Utilities
{
    public static class TestServerExtensions
    {
        public static async Task<ClientQueryResult> PostAsync(
            this TestServer testServer,
            ClientQueryRequest request,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendPostRequestAsync(
                    testServer,
                    JsonConvert.SerializeObject(request),
                    path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
            }

            var json = await response.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert.DeserializeObject<ClientQueryResult>(json);
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType.ToString();
            return result;
        }

        public static async Task<IReadOnlyList<ClientQueryResult>> PostAsync(
            this TestServer testServer,
            IReadOnlyList<ClientQueryRequest> request,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendPostRequestAsync(
                    testServer,
                    JsonConvert.SerializeObject(request),
                    path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new[] { new ClientQueryResult { StatusCode = HttpStatusCode.NotFound } };
            }

            var json = await response.Content.ReadAsStringAsync();
            List<ClientQueryResult> result =
                JsonConvert.DeserializeObject<List<ClientQueryResult>>(json);

            foreach (ClientQueryResult item in result)
            {
                item.StatusCode = response.StatusCode;
                item.ContentType = response.Content.Headers.ContentType.ToString();
            }

            return result;
        }

        public static async Task<IReadOnlyList<ClientQueryResult>> PostOperationAsync(
            this TestServer testServer,
            ClientQueryRequest request,
            string operationNames,
            string path = "/graphql",
            Func<string, string> createOperationParameter = null)
        {
            createOperationParameter ??= s => "batchOperations=[" + s + "]";
            HttpResponseMessage response =
                await SendPostRequestAsync(
                    testServer,
                    JsonConvert.SerializeObject(request),
                    path + "?" + createOperationParameter(operationNames));

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new[] { new ClientQueryResult { StatusCode = HttpStatusCode.NotFound } };
            }

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                List<ClientQueryResult> result =
                    JsonConvert.DeserializeObject<List<ClientQueryResult>>(json);

                foreach (ClientQueryResult item in result)
                {
                    item.StatusCode = response.StatusCode;
                    item.ContentType = response.Content.Headers.ContentType.ToString();
                }

                return result;
            }
            catch
            {
                ClientQueryResult result = JsonConvert.DeserializeObject<ClientQueryResult>(json);
                result.StatusCode = response.StatusCode;
                result.ContentType = response.Content.Headers.ContentType.ToString();
                return new[] { result };
            }
        }

        public static async Task<ClientQueryResult> PostAsync(
            this TestServer testServer,
            string requestJson,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendPostRequestAsync(
                    testServer,
                    requestJson,
                    path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
            }

            var json = await response.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert.DeserializeObject<ClientQueryResult>(json);
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType.ToString();
            return result;
        }

        public static async Task<ClientQueryResult> PostMultipartAsync(
            this TestServer testServer,
            MultipartFormDataContent content,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendMultipartRequestAsync(
                    testServer,
                    content,
                    path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
            }

            var json = await response.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert.DeserializeObject<ClientQueryResult>(json);
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType.ToString();
            return result;
        }

        public static async Task<ClientRawResult> PostRawAsync(
            this TestServer testServer,
            ClientQueryRequest request,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendPostRequestAsync(
                    testServer,
                    JsonConvert.SerializeObject(request),
                    path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ClientRawResult { StatusCode = HttpStatusCode.NotFound };
            }

            return new ClientRawResult
            {
                StatusCode = response.StatusCode,
                ContentType = response.Content.Headers.ContentType!.ToString(),
                Content = await response.Content.ReadAsStringAsync()
            };
        }

        public static async Task<ClientQueryResult> GetAsync(
            this TestServer testServer,
            ClientQueryRequest request,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendGetRequestAsync(testServer, request.ToString().Replace("+", "%2B"), path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
            }

            var json = await response.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert.DeserializeObject<ClientQueryResult>(json);
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType.ToString();
            return result;
        }

        public static async Task<ClientQueryResult> GetActivePersistedQueryAsync(
            this TestServer testServer,
            string hashName,
            string hash,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendGetRequestAsync(
                    testServer,
                    $"extensions={{\"persistedQuery\":{{\"version\":1,\"{hashName}\":\"{hash}\"}}}}",
                    path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
            }

            var json = await response.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert.DeserializeObject<ClientQueryResult>(json);
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType.ToString();
            return result;
        }

        public static async Task<ClientQueryResult> GetStoreActivePersistedQueryAsync(
            this TestServer testServer,
            string query,
            string hashName,
            string hash,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendGetRequestAsync(
                    testServer,
                    $"query={query}&" +
                    "extensions={\"persistedQuery\":{\"version\":1," +
                    $"\"{hashName}\":\"{hash}\"}}}}",
                    path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
            }

            var json = await response.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert.DeserializeObject<ClientQueryResult>(json);
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType.ToString();
            return result;
        }

        public static Task<HttpResponseMessage> SendMultipartRequestAsync(
            this TestServer testServer, MultipartFormDataContent content,
            string path)
        {
            return testServer.CreateClient()
                .PostAsync(CreateUrl(path),
                    content);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync<TObject>(
            this TestServer testServer, TObject requestBody, string path = "/graphql")
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
            return testServer.CreateClient()
                .PostAsync(CreateUrl(path),
                    new StringContent(requestBody,
                        Encoding.UTF8, contentType));
        }

        public static Task<HttpResponseMessage> SendGetRequestAsync(
            this TestServer testServer, string query, string path = null)
        {
            return testServer.CreateClient()
                .GetAsync($"{CreateUrl(path)}/?{query}");
        }

        public static string CreateUrl(string path)
        {
            var url = "http://localhost:5000";

            if (path != null)
            {
                url += "/" + path.TrimStart('/');
            }

            return url;
        }
    }
}

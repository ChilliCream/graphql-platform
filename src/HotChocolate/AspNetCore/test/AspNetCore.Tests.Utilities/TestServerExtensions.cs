using System.Net;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public static class TestServerExtensions
{
    public static async Task<ClientQueryResult> PostAsync(
        this TestServer testServer,
        ClientQueryRequest request,
        string path = "/graphql",
        bool includeQueryPlan = false)
    {
        var response =
            await SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(request),
                path,
                includeQueryPlan);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, };
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType!.ToString();
        return result;
    }

    public static async Task<IReadOnlyList<ClientQueryResult>> PostAsync(
        this TestServer testServer,
        IReadOnlyList<ClientQueryRequest> request,
        string path = "/graphql")
    {
        var response =
            await SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(request),
                path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new[] { new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, }, };
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var reader = new MultipartReader("-", stream);
        var result = new List<ClientQueryResult>();
        MultipartSection? section;

        do
        {
            section = await reader.ReadNextSectionAsync();

            if (section is not null)
            {
                await using (section.Body)
                {
                    using var mem = new MemoryStream();
                    await section.Body.CopyToAsync(mem);

                    var item =
                        JsonConvert.DeserializeObject<ClientQueryResult>(
                            Encoding.UTF8.GetString(mem.ToArray()))!;
                    item.ContentType = section.ContentType;
                    item.StatusCode = response.StatusCode;
                    result.Add(item);
                }
            }
        } while (section is not null);

        return result;
    }

    public static async Task<IReadOnlyList<ClientQueryResult>> PostOperationAsync(
        this TestServer testServer,
        ClientQueryRequest request,
        string operationNames,
        string path = "/graphql",
        Func<string, string>? createOperationParameter = null)
    {
        createOperationParameter ??= s => "batchOperations=[" + s + "]";
        var response =
            await SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(request),
                path + "?" + createOperationParameter(operationNames));

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new[] { new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, }, };
        }

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new MultipartReader("-", stream);
            var result = new List<ClientQueryResult>();
            MultipartSection? section;

            do
            {
                section = await reader.ReadNextSectionAsync();

                if (section is not null)
                {
                    await using (section.Body)
                    {
                        using var mem = new MemoryStream();
                        await section.Body.CopyToAsync(mem);

                        var item =
                            JsonConvert.DeserializeObject<ClientQueryResult>(
                                Encoding.UTF8.GetString(mem.ToArray()))!;
                        item.ContentType = section.ContentType;
                        item.StatusCode = response.StatusCode;
                        result.Add(item);
                    }
                }
            } while (section is not null);

            return result;
        }
        catch
        {
            var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
            result.StatusCode = response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType?.ToString();
            return new[] { result, };
        }
    }

    public static async Task<ClientQueryResult> PostAsync(
        this TestServer testServer,
        string requestJson,
        string path = "/graphql")
    {
        var response =
            await SendPostRequestAsync(
                testServer,
                requestJson,
                path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, };
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType?.ToString();
        return result;
    }

    public static async Task<ClientQueryResult> PostMultipartAsync(
        this TestServer testServer,
        MultipartFormDataContent content,
        string path = "/graphql")
    {
        var response =
            await SendMultipartRequestAsync(
                testServer,
                content,
                path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, };
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType?.ToString();
        return result;
    }

    public static async Task<ClientRawResult> PostRawAsync(
        this TestServer testServer,
        ClientQueryRequest request,
        string path = "/graphql")
    {
        var response =
            await SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(request),
                path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientRawResult { StatusCode = HttpStatusCode.NotFound, };
        }

        return new ClientRawResult
        {
            StatusCode = response.StatusCode,
            ContentType = response.Content.Headers.ContentType!.ToString(),
            Content = await response.Content.ReadAsStringAsync(),
        };
    }

    public static async Task<HttpResponseMessage> PostHttpAsync(
        this TestServer testServer,
        ClientQueryRequest request,
        string path = "/graphql")
    {
        var response =
            await SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(request),
                path);

        return response;
    }

    public static async Task<ClientQueryResult> GetAsync(
        this TestServer testServer,
        ClientQueryRequest request,
        string path = "/graphql")
    {
        var response = await SendGetRequestAsync(testServer, request.ToString().Replace("+", "%2B"), path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, };
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType?.ToString();
        return result;
    }

    public static async Task<ClientQueryResult> GetActivePersistedQueryAsync(
        this TestServer testServer,
        string hashName,
        string hash,
        string path = "/graphql")
    {
        var response =
            await SendGetRequestAsync(
                testServer,
                $"extensions={{\"persistedQuery\":{{\"version\":1,\"{hashName}\":\"{hash}\"}}}}",
                path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, };
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType?.ToString();
        return result;
    }

    public static async Task<ClientQueryResult> GetStoreActivePersistedQueryAsync(
        this TestServer testServer,
        string query,
        string hashName,
        string hash,
        string path = "/graphql")
    {
        var response =
            await SendGetRequestAsync(
                testServer,
                $"query={query}&" +
                "extensions={\"persistedQuery\":{\"version\":1," +
                $"\"{hashName}\":\"{hash}\"}}}}",
                path);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound, };
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType?.ToString();
        return result;
    }

    public static Task<HttpResponseMessage> SendMultipartRequestAsync(
        this TestServer testServer,
        MultipartFormDataContent content,
        string path)
    {
        return testServer.CreateClient().PostAsync(CreateUrl(path), content);
    }

    public static Task<HttpResponseMessage> SendPostRequestAsync<TObject>(
        this TestServer testServer,
        TObject requestBody,
        string path = "/graphql",
        bool includeQueryPlan = false) =>
        SendPostRequestAsync(
            testServer,
            JsonConvert.SerializeObject(requestBody),
            path,
            includeQueryPlan);

    public static Task<HttpResponseMessage> SendPostRequestAsync(
        this TestServer testServer,
        string requestBody,
        string? path = null,
        bool includeQueryPlan = false) =>
        SendPostRequestAsync(
            testServer,
            requestBody,
            "application/json",
            path,
            includeQueryPlan);

    public static Task<HttpResponseMessage> SendPostRequestAsync(
        this TestServer testServer,
        string requestBody,
        string contentType,
        string? path,
        bool includeQueryPlan = false)
    {
        var content = new StringContent(requestBody, Encoding.UTF8, contentType);

        if (includeQueryPlan)
        {
            content.Headers.Add(HttpHeaderKeys.QueryPlan, HttpHeaderValues.IncludeQueryPlan);
        }

        return testServer.CreateClient().PostAsync(CreateUrl(path), content);
    }

    public static Task<HttpResponseMessage> SendGetRequestAsync(
        this TestServer testServer,
        string query,
        string? path = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, $"{CreateUrl(path)}/?{query}");
        message.Headers.Add(HttpHeaderKeys.Preflight, "1");
        return testServer.CreateClient().SendAsync(message);
    }

    public static string CreateUrl(string? path)
    {
        var url = "http://localhost:5000";

        if (path is not null)
        {
            url += "/" + path.TrimStart('/');
        }

        return url;
    }
}

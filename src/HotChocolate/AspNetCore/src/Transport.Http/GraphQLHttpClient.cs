using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Abstractions;
using HotChocolate.Transport.Http.Helper;

namespace HotChocolate.Transport.Http;

/// <inheritdoc />
public class GraphQLHttpClient : IGraphQLHttpClient
{
    private readonly HttpClient _httpClient;

    public GraphQLHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public Task<OperationResult> ExecuteGetAsync(OperationRequest request, CancellationToken cancellationToken)
    {
        var parameter = GetQueryParameter(request);
        var queryString = GetQueryString(parameter);
        var requestUri = $"{_httpClient.BaseAddress}?{queryString}";
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(requestUri)
        };
        requestMessage.AddDefaultAcceptHeaders();
        return SendHttpRequestMessageAsync(requestMessage, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResult> ExecutePostAsync(OperationRequest request, CancellationToken cancellationToken)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = _httpClient.BaseAddress
        };
        requestMessage
            .AddDefaultAcceptHeaders()
            .AddJsonBody(request);
        return SendHttpRequestMessageAsync(requestMessage, cancellationToken);
    }

    private async Task<OperationResult> SendHttpRequestMessageAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var httpResponseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!httpResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Response indicates failure: {httpResponseMessage.StatusCode} {httpResponseMessage.ReasonPhrase}");

        var json = await httpResponseMessage.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(json);
        var operationResult = new OperationResult(
            jsonDocument,
            jsonDocument.RootElement.TryGetProperty("data", out var data) ? data : default,
            jsonDocument.RootElement.TryGetProperty("errors", out var errors) ? errors : default,
            jsonDocument.RootElement.TryGetProperty("extensions", out var extensions) ? extensions : default);
        return operationResult;
    }

    private static string GetQueryString(NameValueCollection valueCollection)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < valueCollection.Count; i++)
        {
            sb.Append($"{valueCollection.Keys[i]}={valueCollection[i]}");
            if (i + 1 < valueCollection.Count)
            {
                sb.Append('&');
            }
        }

        return sb.ToString();
    }

    private static NameValueCollection GetQueryParameter(OperationRequest request)
    {
        var queryParameter = new NameValueCollection();

        if (request.OperationName is not null)
        {
            queryParameter["operationName"] = Uri.EscapeDataString(request.OperationName);
        }

        if (request.Query is not null)
        {
            queryParameter["query"] = Uri.EscapeDataString(request.Query);
        }

        if (request.Variables is not null)
        {
            var variablesObject = JsonSerializer.Serialize(request.Variables);
            queryParameter["variables"] = Uri.EscapeDataString(variablesObject);
        }

        if (request.Extensions is not null)
        {
            var extensionsObject = JsonSerializer.Serialize(request.Extensions);
            queryParameter["extensions"] = Uri.EscapeDataString(extensionsObject);
        }

        return queryParameter;
    }
}

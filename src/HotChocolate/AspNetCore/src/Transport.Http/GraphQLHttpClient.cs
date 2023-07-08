using System;
using System.Net.Http;
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
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get
        };
        requestMessage.AddDefaultAcceptHeaders();
        return SendHttpRequestMessageAsync(requestMessage, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResult> ExecutePostAsync(OperationRequest request, CancellationToken cancellationToken)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post
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
}

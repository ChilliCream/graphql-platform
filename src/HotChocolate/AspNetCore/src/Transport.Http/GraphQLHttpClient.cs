using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
                $"Response indicated failure: {httpResponseMessage.StatusCode} {httpResponseMessage.ReasonPhrase}");

        using var resultStream = await httpResponseMessage.Content.ReadAsStreamAsync();
        var operationResult = JsonSerializer.Deserialize<OperationResult>(resultStream);
        return operationResult ?? throw new InvalidOperationException("Result data is empty");
    }
}

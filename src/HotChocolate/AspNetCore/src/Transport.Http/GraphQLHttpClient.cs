using System.Text;
using System.Text.Json;
using HotChocolate.Transport.Abstractions;

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
        AddAcceptHeader(requestMessage);
        return SendHttpRequestMessageAsync(requestMessage, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResult> ExecutePostAsync(OperationRequest request, CancellationToken cancellationToken)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post
        };
        AddAcceptHeader(requestMessage);
        AddBody(requestMessage, request);
        requestMessage.Headers.Add("Content-Type", "application/json");
        return SendHttpRequestMessageAsync(requestMessage, cancellationToken);
    }

    private async Task<OperationResult> SendHttpRequestMessageAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var httpResponseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            await using var resultStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken);
            var operationResult = JsonSerializer.Deserialize<OperationResult>(resultStream);
            return operationResult ?? throw new InvalidOperationException("Result data is empty");
        }

        throw new InvalidOperationException(
            $"Response indicated failure: {httpResponseMessage.StatusCode} {httpResponseMessage.ReasonPhrase}");
    }

    private static void AddBody(HttpRequestMessage requestMessage, OperationRequest request)
    {
        requestMessage.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(request));
    }

    private static void AddAcceptHeader(HttpRequestMessage requestMessage)
    {
        requestMessage.Headers.Add("Accept", "application/graphql-response+json; charset=utf-8, application/json; charset=utf-8");
    }
}

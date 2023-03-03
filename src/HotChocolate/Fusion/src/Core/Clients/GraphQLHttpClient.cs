using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Clients;

// note: should the GraphQL client handle the capabilities?
// meaning the execution engine should just use batching and
// all and the client decides to batch if batching is available?
public sealed class GraphQLHttpClient : IGraphQLClient
{
    private readonly JsonRequestFormatter _formatter = new();
    private readonly HttpClient _client;

    public GraphQLHttpClient(string schemaName, IHttpClientFactory httpClientFactory)
    {
        SubgraphName = schemaName;
        _client = httpClientFactory.CreateClient(SubgraphName);
    }

    // TODO: naming? SubgraphName?
    public string SubgraphName { get; }

    public async Task<GraphQLResponse> ExecuteAsync(
        GraphQLRequest request,
        CancellationToken cancellationToken)
    {
        // todo : this is just a naive dummy implementation
        using var writer = new ArrayWriter();
        using var requestMessage = CreateRequestMessage(writer, request);
        using var responseMessage = await _client.SendAsync(requestMessage, cancellationToken);

        // responseMessage.EnsureSuccessStatusCode(); // TODO : remove for production

        await using var contentStream = await responseMessage.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var stream = contentStream;

        var sourceEncoding = GetEncoding(responseMessage.Content.Headers.ContentType?.CharSet);

        if (sourceEncoding is not null &&
            !Equals(sourceEncoding.EncodingName, Encoding.UTF8.EncodingName))
        {
            stream = GetTranscodingStream(contentStream, sourceEncoding);
        }

        var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return new GraphQLResponse(document);
    }

    public Task<IAsyncEnumerable<GraphQLResponse>> ExecuteBatchAsync(
        IReadOnlyList<GraphQLRequest> requests,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IAsyncEnumerable<GraphQLResponse>> SubscribeAsync(
        GraphQLRequest graphQLRequests,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private HttpRequestMessage CreateRequestMessage(ArrayWriter writer, GraphQLRequest request)
    {
        _formatter.Write(writer, request);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, default(Uri));
        requestMessage.Content = new ByteArrayContent(writer.GetInternalBuffer(), 0, writer.Length);
        requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return requestMessage;
    }

    private static Encoding? GetEncoding(string? charset)
    {
        Encoding? encoding = null;

        if (charset != null)
        {
            try
            {
                // Remove at most a single set of quotes.
                if (charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"')
                {
                    encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                }
                else
                {
                    encoding = Encoding.GetEncoding(charset);
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException("Invalid Charset", e);
            }

            Debug.Assert(encoding != null);
        }

        return encoding;
    }

    private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
    {
        return Encoding.CreateTranscodingStream(
            contentStream,
            innerStreamEncoding: sourceEncoding,
            outerStreamEncoding: Encoding.UTF8);
    }
}

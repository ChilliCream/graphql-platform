using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Clients;

public sealed class GraphQLHttpClient : IGraphQLClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonRequestFormatter _formatter = new();

    public GraphQLHttpClient(string schemaName, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        SchemaName = schemaName;
    }

    public string SchemaName { get; }

    public async Task<GraphQLResponse> ExecuteAsync(GraphQLRequest request, CancellationToken cancellationToken)
    {
        // todo : this is just a naive dummy implementation
        using var writer = new ArrayWriter();
        using var client = _httpClientFactory.CreateClient(SchemaName);
        using var requestMessage = CreateRequestMessage(writer, request);
        using var responseMessage = await client.SendAsync(requestMessage, cancellationToken);
        var s = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        responseMessage.EnsureSuccessStatusCode(); // TODO : remove for production

        await using var contentStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
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
        return Encoding.CreateTranscodingStream(contentStream, innerStreamEncoding: sourceEncoding, outerStreamEncoding: Encoding.UTF8);
    }
}

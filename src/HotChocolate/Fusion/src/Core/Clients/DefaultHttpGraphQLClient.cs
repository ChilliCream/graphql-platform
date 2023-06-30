using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Clients;

internal sealed class DefaultHttpGraphQLClient : IGraphQLClient
{
    private const string _jsonMediaType = "application/json";
    private const string _graphqlMediaType = "application/graphql-response+json";
    private static readonly Encoding _utf8 = Encoding.UTF8;
    private static readonly GraphQLResponse _transportError = new(CreateTransportError());
    private readonly JsonRequestFormatter _formatter = new();
    private readonly HttpClientConfiguration _config;
    private readonly HttpClient _client;

    public DefaultHttpGraphQLClient(
        HttpClientConfiguration configuration,
        HttpClient httpClient)
    {
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string SubgraphName => _config.SubgraphName;

    public Task<GraphQLResponse> ExecuteAsync(
        GraphQLRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return ExecuteInternalAsync(request, cancellationToken);
    }

    private async Task<GraphQLResponse> ExecuteInternalAsync(
        GraphQLRequest request,
        CancellationToken ct)
    {
        try
        {
            using var writer = new ArrayWriter();
            using var requestMessage = CreateRequestMessage(writer, request);
            using var responseMessage = await _client.SendAsync(requestMessage, ct);

            await using var contentStream = await responseMessage.Content
                .ReadAsStreamAsync(ct)
                .ConfigureAwait(false);

            var stream = contentStream;
            var contentType = responseMessage.Content.Headers.ContentType;
            var sourceEncoding = GetEncoding(contentType?.CharSet);

            if (sourceEncoding is not null && !Equals(sourceEncoding.EncodingName, _utf8.EncodingName))
            {
                stream = GetTranscodingStream(contentStream, sourceEncoding);
            }

            if (contentType?.MediaType.EqualsOrdinal(_jsonMediaType) ?? false)
            {
                responseMessage.EnsureSuccessStatusCode();
                var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return new GraphQLResponse(document);
            }

            if (contentType?.MediaType.EqualsOrdinal(_graphqlMediaType) ?? false)
            {
                var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return new GraphQLResponse(document);
            }

            return _transportError;
        }
        catch
        {
            return _transportError;
        }
    }

    private HttpRequestMessage CreateRequestMessage(ArrayWriter writer, GraphQLRequest request)
    {
        _formatter.Write(writer, request);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, default(Uri));
        requestMessage.RequestUri = _config.EndpointUri;
        requestMessage.Content = new ByteArrayContent(writer.GetInternalBuffer(), 0, writer.Length);
        requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(_jsonMediaType);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_graphqlMediaType));
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_jsonMediaType));
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
        => Encoding.CreateTranscodingStream(
            contentStream,
            innerStreamEncoding: sourceEncoding,
            outerStreamEncoding: _utf8);

    private static JsonElement CreateTransportError()
    {
        return JsonDocument.Parse(
            """
            [{"message": "Internal Execution Error"}]
            """).RootElement;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return default;
    }
}

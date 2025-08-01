using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HotChocolate.Transport.Http;

/// <summary>
/// Represents a GraphQL over HTTP response.
/// </summary>
public sealed class GraphQLHttpResponse : IDisposable
{
    private static readonly OperationResult s_transportError = CreateTransportError();

    private readonly HttpResponseMessage _message;

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLHttpResponse"/>.
    /// </summary>
    /// <param name="message">
    /// The underlying HTTP response message.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="message"/> is <see langword="null"/>.
    /// </exception>
    public GraphQLHttpResponse(HttpResponseMessage message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    /// Gets the underlying <see cref="HttpResponseMessage"/>.
    /// </summary>
    public HttpResponseMessage HttpResponseMessage => _message;

    /// <summary>
    /// Gets the HTTP response version.
    /// </summary>
    public Version Version => _message.Version;

    /// <summary>
    /// Gets the HTTP response status code.
    /// </summary>
    public HttpStatusCode StatusCode => _message.StatusCode;

    /// <summary>
    /// Specifies whether the HTTP response was successful.
    /// </summary>
    public bool IsSuccessStatusCode => _message.IsSuccessStatusCode;

    /// <summary>
    /// Gets the reason phrase which typically is sent by servers together with the status code.
    /// </summary>
    public string? ReasonPhrase => _message.ReasonPhrase;

    /// <summary>
    /// Throws an exception if the HTTP response was unsuccessful.
    /// </summary>
    public void EnsureSuccessStatusCode() => _message.EnsureSuccessStatusCode();

    /// <summary>
    /// Gets the collection of HTTP response headers.
    /// </summary>
    /// <returns>
    /// The collection of HTTP response headers.
    /// </returns>
    public HttpResponseHeaders Headers => _message.Headers;

    /// <summary>
    /// Gets the HTTP content headers as defined in RFC 2616.
    /// </summary>
    /// <returns>
    /// The content headers as defined in RFC 2616.
    /// </returns>
    public HttpContentHeaders ContentHeaders => _message.Content.Headers;

    /// <summary>
    /// Gets the collection of trailing headers included in an HTTP response.
    /// </summary>
    /// <exception cref="T:System.Net.Http.HttpRequestException">
    /// PROTOCOL_ERROR: The HTTP/2 response contains pseudo-headers in the Trailing Headers Frame.
    /// </exception>
    /// <returns>
    /// The collection of trailing headers in the HTTP response.
    /// </returns>
    public HttpResponseHeaders TrailingHeaders => _message.TrailingHeaders;

    /// <summary>
    /// Reads the GraphQL response as a <see cref="OperationResult"/>.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the HTTP request.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous read operation
    /// to read the <see cref="OperationResult"/> from the underlying <see cref="HttpResponseMessage"/>.
    /// </returns>
    public ValueTask<OperationResult> ReadAsResultAsync(CancellationToken cancellationToken = default)
    {
        var contentType = _message.Content.Headers.ContentType;

        // The server supports the newer graphql-response+json media type, and users are free
        // to use status codes.
        if (contentType?.MediaType?.Equals(ContentType.GraphQL, StringComparison.Ordinal) ?? false)
        {
            return ReadAsResultInternalAsync(contentType.CharSet, cancellationToken);
        }

        // The server supports the older application/json media type, and the status code
        // is expected to be a 2xx for a valid GraphQL response.
        if (contentType?.MediaType?.Equals(ContentType.Json, StringComparison.Ordinal) ?? false)
        {
            _message.EnsureSuccessStatusCode();
            return ReadAsResultInternalAsync(contentType.CharSet, cancellationToken);
        }

        _message.EnsureSuccessStatusCode();

        throw new InvalidOperationException("Received a successful response with an unexpected content type.");
    }

    private async ValueTask<OperationResult> ReadAsResultInternalAsync(string? charSet, CancellationToken ct)
    {
        await using var contentStream = await _message.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        var stream = contentStream;

        var sourceEncoding = HttpTransportUtilities.GetEncoding(charSet);
        if (HttpTransportUtilities.NeedsTranscoding(sourceEncoding))
        {
            stream = HttpTransportUtilities.GetTranscodingStream(contentStream, sourceEncoding);
        }

        var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        try
        {
            return OperationResult.Parse(document);
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Reads the GraphQL response as a <see cref="IAsyncEnumerable{T}"/> of <see cref="OperationResult"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="IAsyncEnumerable{T}"/> of <see cref="OperationResult"/> that represents the asynchronous
    /// read operation to read the stream of <see cref="OperationResult"/>s from the underlying
    /// <see cref="HttpResponseMessage"/>.
    /// </returns>
    public IAsyncEnumerable<OperationResult> ReadAsResultStreamAsync()
    {
        var contentType = _message.Content.Headers.ContentType;

        if (contentType?.MediaType?.Equals(ContentType.EventStream, StringComparison.Ordinal) ?? false)
        {
            return new GraphQLHttpEventStreamEnumerable(_message);
        }

        // The server supports the newer graphql-response+json media type, and users are free
        // to use status codes.
        if (contentType?.MediaType?.Equals(ContentType.GraphQL, StringComparison.Ordinal) ?? false)
        {
            return new GraphQLHttpSingleResultEnumerable(
                ct => ReadAsResultInternalAsync(contentType.CharSet, ct));
        }

        // The server supports the older application/json media type, and the status code
        // is expected to be a 2xx for a valid GraphQL response.
        if (contentType?.MediaType?.Equals(ContentType.Json, StringComparison.Ordinal) ?? false)
        {
            _message.EnsureSuccessStatusCode();
            return new GraphQLHttpSingleResultEnumerable(
                ct => ReadAsResultInternalAsync(contentType.CharSet, ct));
        }

        return SingleResult(new ValueTask<OperationResult>(s_transportError));
    }

    private static async IAsyncEnumerable<OperationResult> SingleResult(ValueTask<OperationResult> result)
    {
        yield return await result.ConfigureAwait(false);
    }

    private static OperationResult CreateTransportError()
        => new OperationResult(
            errors: JsonDocument.Parse(
                """
                [{"message": "Internal Execution Error"}]
                """).RootElement);

    /// <summary>
    /// Disposes the underlying <see cref="HttpResponseMessage"/>.
    /// </summary>
    public void Dispose() => _message.Dispose();
}

using System;
using System.Collections.Generic;
using System.Net;
#if NET6_0_OR_GREATER
using System.Diagnostics;
using System.IO;
#endif
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Text;
#endif
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http;

/// <summary>
/// Represents a GraphQL over HTTP response.
/// </summary>
public sealed class GraphQLHttpResponse : IDisposable
{
    private static readonly OperationResult _transportError = CreateTransportError();

#if NET6_0_OR_GREATER
    private static readonly Encoding _utf8 = Encoding.UTF8;
#endif
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

    #if NET6_0_OR_GREATER
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
    #endif

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

        // The server supports the newer graphql-response+json media type and users are free
        // to use status codes.
        if (contentType?.MediaType.EqualsOrdinal(ContentType.GraphQL) ?? false)
        {
#if NET6_0_OR_GREATER
            return ReadAsResultInternalAsync(contentType.CharSet, cancellationToken);
#else
            return ReadAsResultInternalAsync(cancellationToken);
#endif
        }

        // The server supports the older application/json media type and the status code
        // is expected to be a 2xx for a valid GraphQL response.
        if (contentType?.MediaType.EqualsOrdinal(ContentType.Json) ?? false)
        {
            _message.EnsureSuccessStatusCode();
#if NET6_0_OR_GREATER
            return ReadAsResultInternalAsync(contentType.CharSet, cancellationToken);
#else
            return ReadAsResultInternalAsync(cancellationToken);
#endif
        }

        // if the media type is anything else we will return a transport error.
        return new ValueTask<OperationResult>(_transportError);
    }

#if NET6_0_OR_GREATER
    private async ValueTask<OperationResult> ReadAsResultInternalAsync(string? charSet, CancellationToken ct)
#else
    private async ValueTask<OperationResult> ReadAsResultInternalAsync(CancellationToken ct)
#endif
    {
#if NET6_0_OR_GREATER
        await using var contentStream = await _message.Content.ReadAsStreamAsync(ct)
            .ConfigureAwait(false);
#else
        using var contentStream = await _message.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        var stream = contentStream;

#if NET6_0_OR_GREATER
        var sourceEncoding = GetEncoding(charSet);
        if (sourceEncoding is not null && !Equals(sourceEncoding.EncodingName, _utf8.EncodingName))
        {
            stream = GetTranscodingStream(contentStream, sourceEncoding);
        }
#endif

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
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the HTTP request.
    /// </param>
    /// <returns>
    /// A <see cref="IAsyncEnumerable{T}"/> of <see cref="OperationResult"/> that represents the asynchronous
    /// read operation to read the stream of <see cref="OperationResult"/>s from the underlying
    /// <see cref="HttpResponseMessage"/>.
    /// </returns>
    public IAsyncEnumerable<OperationResult> ReadAsResultStreamAsync(CancellationToken cancellationToken = default)
    {
        var contentType = _message.Content.Headers.ContentType;

        if (contentType?.MediaType.EqualsOrdinal(ContentType.EventStream) ?? false)
        {
#if NET6_0_OR_GREATER
            return ReadAsResultStreamInternalAsync(contentType.CharSet, cancellationToken);
#else
            return ReadAsResultStreamInternalAsync(cancellationToken);
#endif
        }

        // The server supports the newer graphql-response+json media type and users are free
        // to use status codes.
        if (contentType?.MediaType.EqualsOrdinal(ContentType.GraphQL) ?? false)
        {
#if NET6_0_OR_GREATER
            return SingleResult(ReadAsResultInternalAsync(contentType.CharSet, cancellationToken));
#else
            return SingleResult(ReadAsResultInternalAsync(cancellationToken));
#endif
        }

        // The server supports the older application/json media type and the status code
        // is expected to be a 2xx for a valid GraphQL response.
        if (contentType?.MediaType.EqualsOrdinal(ContentType.Json) ?? false)
        {
            _message.EnsureSuccessStatusCode();
#if NET6_0_OR_GREATER
            return SingleResult(ReadAsResultInternalAsync(contentType.CharSet, cancellationToken));
#else
            return SingleResult(ReadAsResultInternalAsync(cancellationToken));
#endif
        }

        return SingleResult(new ValueTask<OperationResult>(_transportError));
    }

#if NET6_0_OR_GREATER
    private async IAsyncEnumerable<OperationResult> ReadAsResultStreamInternalAsync(
        string? charSet,
        [EnumeratorCancellation] CancellationToken ct)
#else
    private async IAsyncEnumerable<OperationResult> ReadAsResultStreamInternalAsync(
        [EnumeratorCancellation] CancellationToken ct)
#endif
    {
#if NET6_0_OR_GREATER
        await using var contentStream = await _message.Content.ReadAsStreamAsync(ct)
            .ConfigureAwait(false);
#else
        using var contentStream = await _message.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        var stream = contentStream;

#if NET6_0_OR_GREATER
        var sourceEncoding = GetEncoding(charSet);
        if (sourceEncoding is not null && !Equals(sourceEncoding.EncodingName, _utf8.EncodingName))
        {
            stream = GetTranscodingStream(contentStream, sourceEncoding);
        }
#endif

        await foreach (var item in GraphQLHttpEventStreamProcessor.ReadStream(stream, ct).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    private static async IAsyncEnumerable<OperationResult> SingleResult(ValueTask<OperationResult> result)
    {
        yield return await result.ConfigureAwait(false);
    }

#if NET6_0_OR_GREATER
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
#endif

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

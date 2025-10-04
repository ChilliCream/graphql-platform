using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
#if FUSION
using System.IO.Pipelines;
using HotChocolate.Transport;
using HotChocolate.Fusion.Text.Json;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// Represents a GraphQL over HTTP response.
/// </summary>
public sealed class GraphQLHttpResponse : IDisposable
{
#if FUSION
    private static readonly StreamPipeReaderOptions s_options = new(
        pool: MemoryPool<byte>.Shared,
        bufferSize: 4096,
        minimumReadSize: 1,
        leaveOpen: true,
        useZeroByteReads: true);
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
#if FUSION
    public ValueTask<SourceResultDocument> ReadAsResultAsync(CancellationToken cancellationToken = default)
#else
    public ValueTask<OperationResult> ReadAsResultAsync(CancellationToken cancellationToken = default)
#endif
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

#if FUSION
    private async ValueTask<SourceResultDocument> ReadAsResultInternalAsync(string? charSet, CancellationToken ct)
#else
    private async ValueTask<OperationResult> ReadAsResultInternalAsync(string? charSet, CancellationToken ct)
#endif
    {
        await using var contentStream = await _message.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        var stream = contentStream;

        var sourceEncoding = HttpTransportUtilities.GetEncoding(charSet);
        if (HttpTransportUtilities.NeedsTranscoding(sourceEncoding))
        {
            stream = HttpTransportUtilities.GetTranscodingStream(contentStream, sourceEncoding);
        }

#if FUSION
        // we try and read the first chunk into a single chunk.
        var reader = PipeReader.Create(stream, s_options);
        var currentChunk = JsonMemory.Rent(); // 128KB chunk
        var currentChunkPosition = 0;
        List<byte[]>? chunks = null;

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(ct);
                var buffer = result.Buffer;

                if (buffer.IsEmpty && result.IsCompleted)
                {
                    break;
                }

                // Process each segment in the buffer
                foreach (var segment in buffer)
                {
                    var segmentSpan = segment.Span;
                    var segmentOffset = 0;

                    while (segmentOffset < segmentSpan.Length)
                    {
                        var spaceInCurrentChunk = JsonMemory.ChunkSize - currentChunkPosition;
                        var bytesToCopy = Math.Min(spaceInCurrentChunk, segmentSpan.Length - segmentOffset);

                        // we copy the data we have into the current chunk.
                        var chunkSlice = currentChunk.AsSpan(currentChunkPosition);
                        segmentSpan.Slice(segmentOffset, bytesToCopy).CopyTo(chunkSlice);
                        currentChunkPosition += bytesToCopy;
                        segmentOffset += bytesToCopy;

                        // if the current chunk is full, we need to get a new one
                        // and store the chunk in the chunk list
                        if (currentChunkPosition == JsonMemory.ChunkSize)
                        {
                            chunks ??= [];
                            chunks.Add(currentChunk);
                            currentChunk = JsonMemory.Rent();
                            currentChunkPosition = 0;
                        }
                    }
                }

                reader.AdvanceTo(buffer.End);
            }

            // if we do not have a chunk list everything has fit into out initial chunk.
            // this means we do not need to allocate any memory and can use the optimized parse.
            if (chunks is null)
            {
                return SourceResultDocument.Parse(currentChunk, size: currentChunkPosition);
            }

            // add the final partial chunk to the list
            chunks.Add(currentChunk);
            return SourceResultDocument.Parse(chunks.ToArray(), currentChunkPosition);
        }
        finally
        {
            await reader.CompleteAsync();
        }
#else
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
#endif
    }

    /// <summary>
    /// Reads the GraphQL response as a <see cref="IAsyncEnumerable{T}"/> of <see cref="OperationResult"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="IAsyncEnumerable{T}"/> of <see cref="OperationResult"/> that represents the asynchronous
    /// read operation to read the stream of <see cref="OperationResult"/>s from the underlying
    /// <see cref="HttpResponseMessage"/>.
    /// </returns>
#if FUSION
    public IAsyncEnumerable<SourceResultDocument> ReadAsResultStreamAsync()
#else
    public IAsyncEnumerable<OperationResult> ReadAsResultStreamAsync()
#endif
    {
        var contentType = _message.Content.Headers.ContentType;

        if (contentType?.MediaType?.Equals(ContentType.EventStream, StringComparison.Ordinal) ?? false)
        {
            return new SseReader(_message);
        }

        if ((contentType?.MediaType?.Equals(ContentType.GraphQLJsonLine, StringComparison.Ordinal) ?? false)
            || (contentType?.MediaType?.Equals(ContentType.JsonLine, StringComparison.Ordinal) ?? false))
        {
            return new JsonLinesReader(_message);
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

        _message.EnsureSuccessStatusCode();

        throw new InvalidOperationException("Received a successful response with an unexpected content type.");
    }

    /// <summary>
    /// Disposes the underlying <see cref="HttpResponseMessage"/>.
    /// </summary>
    public void Dispose() => _message.Dispose();
}

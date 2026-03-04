#if FUSION
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
#else
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Buffers;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// Reads a JSON response that can be either a single object or an array of GraphQL responses.
/// </summary>
#if FUSION
internal sealed class JsonResultEnumerable(HttpResponseMessage message, string? charSet) : IAsyncEnumerable<SourceResultDocument>
#else
internal sealed class JsonResultEnumerable(HttpResponseMessage message, string? charSet) : IAsyncEnumerable<OperationResult>
#endif
{
    private static readonly StreamPipeReaderOptions s_options = new(
        pool: MemoryPool<byte>.Shared,
        bufferSize: 4096,
        minimumReadSize: 1,
        leaveOpen: true,
        useZeroByteReads: true);

#if FUSION
    public async IAsyncEnumerator<SourceResultDocument> GetAsyncEnumerator(
#else
    public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
#endif
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var contentStream = await message.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);

        var stream = contentStream;
        var sourceEncoding = HttpTransportUtilities.GetEncoding(charSet);
        if (HttpTransportUtilities.NeedsTranscoding(sourceEncoding))
        {
            stream = HttpTransportUtilities.GetTranscodingStream(contentStream, sourceEncoding);
        }

        var reader = PipeReader.Create(stream, s_options);

#if FUSION
        var chunks = ArrayPool<byte[]>.Shared.Rent(64);
        var currentChunk = JsonMemory.Rent(JsonMemoryKind.Json);
        var currentChunkPosition = 0;
        var chunkIndex = 0;
#else
        var buffer = new PooledArrayWriter();
        var bufferOwnershipTransferred = false;
#endif

        try
        {
            // Read the entire response into memory
            while (true)
            {
                var result = await reader.ReadAsync(cts.Token).ConfigureAwait(false);
                if (result.IsCanceled)
                {
                    yield break;
                }

                var pipeBuffer = result.Buffer;

#if FUSION
                foreach (var segment in pipeBuffer)
                {
                    var segmentSpan = segment.Span;
                    var segmentOffset = 0;

                    while (segmentOffset < segmentSpan.Length)
                    {
                        var spaceInCurrentChunk = JsonMemory.BufferSize - currentChunkPosition;
                        var bytesToCopy = Math.Min(spaceInCurrentChunk, segmentSpan.Length - segmentOffset);

                        segmentSpan.Slice(segmentOffset, bytesToCopy).CopyTo(currentChunk.AsSpan(currentChunkPosition));
                        currentChunkPosition += bytesToCopy;
                        segmentOffset += bytesToCopy;

                        if (currentChunkPosition == JsonMemory.BufferSize)
                        {
                            if (chunkIndex >= chunks.Length)
                            {
                                var newChunks = ArrayPool<byte[]>.Shared.Rent(chunks.Length * 2);
                                Array.Copy(chunks, 0, newChunks, 0, chunks.Length);
                                chunks.AsSpan().Clear();
                                ArrayPool<byte[]>.Shared.Return(chunks);
                                chunks = newChunks;
                            }

                            chunks[chunkIndex++] = currentChunk;
                            currentChunk = JsonMemory.Rent(JsonMemoryKind.Json);
                            currentChunkPosition = 0;
                        }
                    }
                }
#else
                foreach (var segment in pipeBuffer)
                {
                    var span = buffer.GetSpan(segment.Length);
                    segment.Span.CopyTo(span);
                    buffer.Advance(segment.Length);
                }
#endif

                reader.AdvanceTo(pipeBuffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

#if FUSION
            // Add the final partial chunk
            if (chunkIndex >= chunks.Length)
            {
                var newChunks = ArrayPool<byte[]>.Shared.Rent(chunks.Length * 2);
                Array.Copy(chunks, 0, newChunks, 0, chunks.Length);
                chunks.AsSpan().Clear();
                ArrayPool<byte[]>.Shared.Return(chunks);
                chunks = newChunks;
            }
            chunks[chunkIndex++] = currentChunk;

            if (IsJsonArray(chunks, chunkIndex, currentChunkPosition))
            {
                Utf8JsonReader jsonReader;
                if (chunkIndex > 1)
                {
                    SequenceSegment? first = null;
                    SequenceSegment? previous = null;
                    var dataChunksSpan = chunks.AsSpan(0, chunkIndex);

                    for (var i = 0; i < dataChunksSpan.Length; i++)
                    {
                        var chunk = dataChunksSpan[i];
                        var chunkDataLength =
 (i == dataChunksSpan.Length - 1) ? currentChunkPosition : JsonMemory.BufferSize;
                        var current = new SequenceSegment(chunk, chunkDataLength);

                        first ??= current;
                        previous?.SetNext(current);
                        previous = current;
                    }

                    if (first is null || previous is null)
                    {
                        throw new InvalidOperationException("Sequence segments cannot be empty.");
                    }

                    var sequence = new ReadOnlySequence<byte>(first, 0, previous, currentChunkPosition);
                    jsonReader = new Utf8JsonReader(sequence, default);
                }
                else
                {
                    jsonReader = new Utf8JsonReader(chunks[0].AsSpan(0, currentChunkPosition), default);
                }

                jsonReader.Read();

                if (jsonReader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Expected first JSON token to be a StartArray.");
                }

                var documents = new List<SourceResultDocument>();

                var isFirstDocument = true;
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    var document = SourceResultDocument.Parse(
                        ref jsonReader,
                        chunks,
                        usedChunks: chunkIndex,
                        skipInitialRead: true,
                        pooledMemory: isFirstDocument);

                    documents.Add(document);

                    isFirstDocument = false;
                }

                foreach (var document in documents)
                {
                    yield return document;
                }
            }
            else
            {
                yield return SourceResultDocument.Parse(
                    chunks,
                    lastLength: currentChunkPosition,
                    usedChunks: chunkIndex,
                    pooledMemory: true);
            }
#else
            var memory = buffer.WrittenMemory;

            if (IsJsonArray(memory.Span))
            {
                var jsonReader = new Utf8JsonReader(memory.Span);
                var documents = new List<JsonDocument>();

                if (!jsonReader.Read() || jsonReader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("Expected first JSON token to be a StartArray.");
                }

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (jsonReader.TokenType == JsonTokenType.StartObject)
                    {
                        var doc = JsonDocument.ParseValue(ref jsonReader);
                        documents.Add(doc);
                    }
                }

                foreach (var document in documents)
                {
                    yield return OperationResult.Parse(document);
                }
            }
            else
            {
                var document = JsonDocument.Parse(memory);
                var documentOwner = new JsonDocumentOwner(document, buffer);
                yield return OperationResult.Parse(documentOwner);

                bufferOwnershipTransferred = true;
            }
#endif
        }
        finally
        {
#if !FUSION
            // If we haven't transferred ownership of the buffer via a JsonDocumentOwner
            // or we've encountered an exception, we need to free the allocated memory.
            if (!bufferOwnershipTransferred)
            {
                buffer.Dispose();
            }
#endif

            await cts.CancelAsync().ConfigureAwait(false);
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

#if FUSION
    private static bool IsJsonArray(byte[][] chunks, int usedChunks, int lastChunkLength)
    {
        for (var i = 0; i < usedChunks; i++)
        {
            var chunkLength = (i == usedChunks - 1) ? lastChunkLength : JsonMemory.BufferSize;
            var chunk = chunks[i].AsSpan(0, chunkLength);

            foreach (var b in chunk)
            {
                // Skip whitespaces.
                if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                {
                    continue;
                }

                return b == (byte)'[';
            }
        }

        return false;
    }
#else
    private static bool IsJsonArray(ReadOnlySpan<byte> span)
    {
        foreach (var b in span)
        {
            if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            {
                continue;
            }

            return b == (byte)'[';
        }

        return false;
    }
#endif
}

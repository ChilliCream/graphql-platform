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
internal sealed class JsonResultEnumerable(HttpResponseMessage message, IMemoryArenaSource arenaSource, string? charSet) : IAsyncEnumerable<SourceResultDocument>
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
        // The array siblings of a JSON array body share segments, so the whole body is structurally
        // one arena: acquire it once before filling.
        var arena = arenaSource.GetNextArena();
        var chunks = arena.RentSegmentTable(64);
        var chunkIndex = 0;
        var chunkSize = SourceResultDocument.GetDataChunkSize(chunkIndex);
        var current = chunks[chunkIndex] = arena.Rent(chunkSize);
        var currentChunkPosition = 0;
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
                    var source = segment.Span;
                    var segmentOffset = 0;

                    while (segmentOffset < source.Length)
                    {
                        var spaceInCurrentChunk = chunkSize - currentChunkPosition;
                        var bytesToCopy = Math.Min(spaceInCurrentChunk, source.Length - segmentOffset);

                        source.Slice(segmentOffset, bytesToCopy).CopyTo(current.Span.Slice(currentChunkPosition));
                        currentChunkPosition += bytesToCopy;
                        segmentOffset += bytesToCopy;

                        if (currentChunkPosition == chunkSize)
                        {
                            if (chunkIndex + 1 >= SourceResultDocument.DataMaxChunks)
                            {
                                throw new InvalidOperationException(
                                    "The source result document has exceeded its maximum data capacity.");
                            }

                            if (chunkIndex + 1 >= chunks.Length)
                            {
                                arena.GrowSegmentTable(ref chunks);
                            }

                            chunkIndex++;
                            chunkSize = SourceResultDocument.GetDataChunkSize(chunkIndex);
                            current = chunks[chunkIndex] = arena.Rent(chunkSize);
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
            var lastLength = currentChunkPosition;
            var usedChunks = chunkIndex + 1;

            if (IsJsonArray(SourceResultDocument.FirstSpan(chunks, usedChunks, lastLength)))
            {
                // All elements parse over the same shared arena segments; their packed data locations
                // point into those segments, so there is no per-element copy. The reader is a ref
                // struct and cannot cross a yield, so the elements are parsed first and yielded after.
                var documents = new List<SourceResultDocument>();
                var jsonReader = SourceResultDocument.CreateFilledReader(chunks, usedChunks, lastLength);

                jsonReader.Read();

                if (jsonReader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Expected first JSON token to be a StartArray.");
                }

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    documents.Add(
                        SourceResultDocument.ParseFilledElement(
                            arena,
                            ref jsonReader,
                            chunks,
                            usedChunks));
                }

                foreach (var document in documents)
                {
                    yield return document;
                }
            }
            else
            {
                yield return SourceResultDocument.ParseFilled(arena, chunks, usedChunks, lastLength);
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
}

#if FUSION
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
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
        var currentChunk = JsonMemory.Rent();
        var currentChunkPosition = 0;
        var chunkIndex = 0;
#else
        var buffer = new PooledArrayWriter();
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
                            currentChunk = JsonMemory.Rent();
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

            // Determine if this is an array or object by finding the first non-whitespace byte
            var isArray = IsJsonArray(chunks, chunkIndex, currentChunkPosition);

            if (isArray)
            {
                // First pass: collect all element ranges
                var elementRanges = CollectElementRanges(chunks, chunkIndex, currentChunkPosition);

                // Second pass: yield each element
                foreach (var (elementStart, elementLength) in elementRanges)
                {
                    var elementChunks = ExtractElement(chunks, chunkIndex, currentChunkPosition, elementStart, elementLength);

                    yield return SourceResultDocument.Parse(
                        elementChunks.Chunks,
                        elementChunks.LastLength,
                        elementChunks.UsedChunks,
                        options: default,
                        pooledMemory: true);
                }

                // Clean up the source chunks since elements were extracted
                for (var i = 0; i < chunkIndex; i++)
                {
                    JsonMemory.Return(chunks[i]);
                }
                ArrayPool<byte[]>.Shared.Return(chunks);
            }
            else
            {
                // Parse as single object - chunks ownership transfers to SourceResultDocument
                yield return SourceResultDocument.Parse(
                    chunks,
                    lastLength: currentChunkPosition,
                    usedChunks: chunkIndex,
                    options: default,
                    pooledMemory: true);
            }
#else
            var memory = buffer.WrittenMemory;

            // Determine if this is an array or object
            var isArray = IsJsonArray(memory.Span);

            if (isArray)
            {
                // First pass: collect all element ranges
                var elementRanges = CollectElementRanges(memory.Span);

                // Second pass: yield each element
                foreach (var (elementStart, elementLength) in elementRanges)
                {
                    var elementBuffer = new PooledArrayWriter(elementLength);
                    var elementSpan = elementBuffer.GetSpan(elementLength);
                    memory.Span.Slice(elementStart, elementLength).CopyTo(elementSpan);
                    elementBuffer.Advance(elementLength);

                    var document = JsonDocument.Parse(elementBuffer.WrittenMemory);
                    var documentOwner = new JsonDocumentOwner(document, elementBuffer);
                    yield return OperationResult.Parse(documentOwner);
                }

                buffer.Dispose();
            }
            else
            {
                // Parse as single object - buffer ownership transfers to JsonDocumentOwner
                var document = JsonDocument.Parse(buffer.WrittenMemory);
                var documentOwner = new JsonDocumentOwner(document, buffer);
                yield return OperationResult.Parse(documentOwner);
            }
#endif
        }
        finally
        {
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
                if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                {
                    continue;
                }

                return b == (byte)'[';
            }
        }

        return false;
    }

    private static List<(long Start, int Length)> CollectElementRanges(byte[][] chunks, int usedChunks, int lastChunkLength)
    {
        var sequence = CreateSequence(chunks, usedChunks, lastChunkLength);
        var jsonReader = new Utf8JsonReader(sequence);
        var ranges = new List<(long Start, int Length)>();

        if (!jsonReader.Read() || jsonReader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException("Expected JSON array.");
        }

        while (jsonReader.Read())
        {
            if (jsonReader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var elementStart = jsonReader.TokenStartIndex;
            SkipCurrentElement(ref jsonReader);
            var elementEnd = jsonReader.BytesConsumed;
            var elementLength = (int)(elementEnd - elementStart);

            ranges.Add((elementStart, elementLength));
        }

        return ranges;
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

    private static List<(int Start, int Length)> CollectElementRanges(ReadOnlySpan<byte> span)
    {
        var jsonReader = new Utf8JsonReader(span);
        var ranges = new List<(int Start, int Length)>();

        if (!jsonReader.Read() || jsonReader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException("Expected JSON array.");
        }

        while (jsonReader.Read())
        {
            if (jsonReader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var elementStart = (int)jsonReader.TokenStartIndex;
            SkipCurrentElement(ref jsonReader);
            var elementEnd = (int)jsonReader.BytesConsumed;
            var elementLength = elementEnd - elementStart;

            ranges.Add((elementStart, elementLength));
        }

        return ranges;
    }
#endif

    private static void SkipCurrentElement(ref Utf8JsonReader reader)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            var depth = reader.CurrentDepth;
            while (reader.Read() && reader.CurrentDepth > depth)
            {
                // Keep reading until we exit the current element
            }
        }
    }

#if FUSION
    private static ReadOnlySequence<byte> CreateSequence(byte[][] chunks, int usedChunks, int lastChunkLength)
    {
        if (usedChunks == 1)
        {
            return new ReadOnlySequence<byte>(chunks[0].AsMemory(0, lastChunkLength));
        }

        SequenceSegment? first = null;
        SequenceSegment? previous = null;

        for (var i = 0; i < usedChunks; i++)
        {
            var chunkLength = (i == usedChunks - 1) ? lastChunkLength : JsonMemory.BufferSize;
            var current = new SequenceSegment(chunks[i], chunkLength);

            first ??= current;
            previous?.SetNext(current);
            previous = current;
        }

        return new ReadOnlySequence<byte>(first!, 0, previous!, lastChunkLength);
    }

    private static (byte[][] Chunks, int LastLength, int UsedChunks) ExtractElement(
        byte[][] sourceChunks,
        int sourceUsedChunks,
        int sourceLastChunkLength,
        long startIndex,
        int length)
    {
        var requiredChunks = (length + JsonMemory.BufferSize - 1) / JsonMemory.BufferSize;
        var elementChunks = JsonMemory.RentRange(requiredChunks);
        var elementChunkIndex = 0;
        var elementChunkPosition = 0;

        var sourcePosition = 0L;
        var bytesRemaining = length;

        for (var i = 0; i < sourceUsedChunks && bytesRemaining > 0; i++)
        {
            var chunkLength = (i == sourceUsedChunks - 1) ? sourceLastChunkLength : JsonMemory.BufferSize;
            var chunkEnd = sourcePosition + chunkLength;

            if (chunkEnd > startIndex)
            {
                var offsetInChunk = (int)Math.Max(0, startIndex - sourcePosition);
                var availableInChunk = chunkLength - offsetInChunk;
                var bytesToCopy = Math.Min(availableInChunk, bytesRemaining);

                var sourceSpan = sourceChunks[i].AsSpan(offsetInChunk, bytesToCopy);
                var copyOffset = 0;

                while (copyOffset < bytesToCopy)
                {
                    var spaceInElementChunk = JsonMemory.BufferSize - elementChunkPosition;
                    var copyLength = Math.Min(spaceInElementChunk, bytesToCopy - copyOffset);

                    sourceSpan.Slice(copyOffset, copyLength).CopyTo(elementChunks[elementChunkIndex].AsSpan(elementChunkPosition));
                    elementChunkPosition += copyLength;
                    copyOffset += copyLength;

                    if (elementChunkPosition == JsonMemory.BufferSize)
                    {
                        elementChunkIndex++;
                        elementChunkPosition = 0;
                    }
                }

                bytesRemaining -= bytesToCopy;
            }

            sourcePosition = chunkEnd;
        }

        var usedChunks = elementChunkPosition > 0 ? elementChunkIndex + 1 : elementChunkIndex;
        var lastLength = elementChunkPosition > 0 ? elementChunkPosition : JsonMemory.BufferSize;

        return (elementChunks, lastLength, usedChunks);
    }

    private sealed class SequenceSegment : ReadOnlySequenceSegment<byte>
    {
        public SequenceSegment(byte[] data, int length)
        {
            Memory = data.AsMemory(0, length);
        }

        public void SetNext(SequenceSegment next)
        {
            next.RunningIndex = RunningIndex + Memory.Length;
            Next = next;
        }
    }
#endif
}

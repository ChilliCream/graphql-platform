using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Transport.Http;

internal class JsonLinesReader(HttpResponseMessage message) : IAsyncEnumerable<OperationResult>
{
    private static readonly StreamPipeReaderOptions s_options = new(
        pool: MemoryPool<byte>.Shared,
        bufferSize: 4096,
        minimumReadSize: 1,
        leaveOpen: true,
        useZeroByteReads: true);

    public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var stream = await message.Content.ReadAsStreamAsync(cts.Token);
        var reader = PipeReader.Create(stream, s_options);

        try
        {
            ReadResult result;
            do
            {
                result = await reader.ReadAsync(cts.Token).ConfigureAwait(false);
                if (result.IsCanceled)
                {
                    yield break;
                }

                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End;

                do
                {
                    var position = buffer.PositionOf((byte)'\n');
                    if (position is null)
                    {
                        // Mark what we've examined but not consumed
                        examined = buffer.End;
                        break;
                    }

                    var line = buffer.Slice(0, position.Value);

                    // Skip empty lines
                    if (!IsEmptyLine(line))
                    {
                        var document = ParseDocument(line);
                        yield return OperationResult.Parse(document);
                    }

                    // Move past the processed line
                    var nextPosition = buffer.GetPosition(1, position.Value);
                    consumed = nextPosition;
                    buffer = buffer.Slice(nextPosition);
                } while (!buffer.IsEmpty);

                // Tell the reader how much we've consumed and examined
                reader.AdvanceTo(consumed, examined);
            } while (!result.IsCompleted);
        }
        finally
        {
            await cts.CancelAsync().ConfigureAwait(false);
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    private static JsonDocumentOwner ParseDocument(ReadOnlySequence<byte> lineBuffer)
    {
        var requiredSize = (int)lineBuffer.Length;
        var buffer = new PooledArrayWriter(requiredSize);
        lineBuffer.CopyTo(buffer.GetSpan(requiredSize));
        buffer.Advance(requiredSize);
        return new JsonDocumentOwner(JsonDocument.Parse(buffer.WrittenMemory), buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEmptyLine(ReadOnlySequence<byte> lineBuffer)
    {
        if (lineBuffer.IsEmpty)
        {
            return true;
        }

        if (lineBuffer.IsSingleSegment)
        {
            var span = lineBuffer.First.Span;
            return IsWhitespaceOnly(span);
        }

        var position = lineBuffer.Start;
        while (lineBuffer.TryGet(ref position, out var memory))
        {
            if (!IsWhitespaceOnly(memory.Span))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespaceOnly(ReadOnlySpan<byte> span)
    {
        foreach (var b in span)
        {
            if (b != (byte)' ' && b != (byte)'\t' && b != (byte)'\r')
            {
                return false;
            }
        }
        return true;
    }
}

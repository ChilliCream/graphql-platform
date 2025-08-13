using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Transport.Http;

internal class SseReader(HttpResponseMessage message) : IAsyncEnumerable<OperationResult>
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
        using var eventBuffer = new PooledArrayWriter(4096);
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

                    WriteLineToMessage(eventBuffer, buffer.Slice(0, position.Value));

                    if (IsMessageComplete(eventBuffer.WrittenSpan))
                    {
                        var eventMessage = SseEventParser.Parse(eventBuffer.WrittenSpan);

                        switch (eventMessage.Type)
                        {
                            case SseEventType.Complete:
                                reader.AdvanceTo(buffer.GetPosition(1, position.Value));
                                yield break;

                            case SseEventType.Next when eventMessage.Data is not null:
                                eventBuffer.Reset();
                                var document = JsonDocument.Parse(eventMessage.Data.WrittenMemory);
                                var documentOwner = new JsonDocumentOwner(document, eventMessage.Data);
                                yield return OperationResult.Parse(documentOwner);
                                break;

                            default:
                                throw new GraphQLHttpStreamException("Malformed message received.");
                        }
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

    private static void WriteLineToMessage(PooledArrayWriter message, ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            var span = buffer.First.Span;

            // normalize line breaks.
            if (span.Length > 0 && span[^1] == (byte)'\r')
            {
                span = span[..^1];
            }

            span.CopyTo(message.GetSpan(span.Length));
            message.Advance(span.Length);
        }
        else
        {
            var position = buffer.Start;
            while (buffer.TryGet(ref position, out var memory))
            {
                var span = memory.Span;

                // normalize line breaks.
                if (position.Equals(buffer.End) && span.Length > 0 && span[^1] == (byte)'\r')
                {
                    span = span[..^1];
                }

                span.CopyTo(message.GetSpan(span.Length));
                message.Advance(span.Length);
            }
        }

        // re-add unified line break (LF only)
        message.GetSpan(1)[0] = (byte)'\n';
        message.Advance(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMessageComplete(ReadOnlySpan<byte> message)
        => message.Length >= 2 && message[^1] == (byte)'\n' && message[^2] == (byte)'\n';
}

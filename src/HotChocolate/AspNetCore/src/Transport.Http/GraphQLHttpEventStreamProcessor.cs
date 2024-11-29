using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http;

internal static class GraphQLHttpEventStreamProcessor
{
    private static ReadOnlySpan<byte> Event => "event:"u8;
    private static ReadOnlySpan<byte> Next => "next\n"u8;
    private static ReadOnlySpan<byte> Complete => "complete\n"u8;
    private static ReadOnlySpan<byte> Data => "data:"u8;

    public static IAsyncEnumerable<OperationResult> ReadStream(Stream stream, CancellationToken ct)
    {
        var pipe = new Pipe();
        var reader = pipe.Reader;
        var writer = pipe.Writer;

        Task.Run(async () => await ReadFromTransportAsync(stream, writer, ct).ConfigureAwait(false), ct);
        return ReadMessagesPipeAsync(reader, ct);
    }

    private static async Task ReadFromTransportAsync(Stream stream, PipeWriter writer, CancellationToken ct)
    {
        const int bufferSize = 64;
        var buffer = new byte[bufferSize];
        var bufferMemory = new Memory<byte>(buffer);

        await using var tokenRegistration = ct.Register(
            static writer => ((PipeWriter)writer!).CancelPendingFlush(),
            state: writer,
            useSynchronizationContext: false);

        while (true)
        {
            try
            {
                var bytesRead = await stream.ReadAsync(bufferMemory, ct).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    break;
                }

                var memory = writer.GetMemory(bytesRead);
                buffer.AsSpan()[..bytesRead].CopyTo(memory.Span);
                writer.Advance(bytesRead);
            }
            catch
            {
                break;
            }

            // ReSharper disable once RedundantArgumentDefaultValue
            var result = await writer.FlushAsync(default).ConfigureAwait(false);
            if (result.IsCompleted || result.IsCanceled)
            {
                break;
            }
        }

        await writer.CompleteAsync().ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<OperationResult> ReadMessagesPipeAsync(
        PipeReader reader,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var message = new ArrayWriter();

        await using var tokenRegistration = ct.Register(
            static reader => ((PipeReader)reader!).CancelPendingRead(),
            state: reader,
            useSynchronizationContext: false);

        while (true)
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            var result = await reader.ReadAsync(default).ConfigureAwait(false);
            if (result.IsCanceled)
            {
                break;
            }

            var buffer = result.Buffer;
            SequencePosition? position;

            do
            {
                position = buffer.PositionOf((byte) '\n');

                if (position == null)
                {
                    continue;
                }

                WriteToMessage(message, buffer.Slice(0, position.Value));

                if (IsMessageComplete(message))
                {
                    if (!TryReadMessage(message.GetWrittenMemory(), out var operationResult))
                    {
                        await reader.CompleteAsync().ConfigureAwait(false);
                        yield break;
                    }

                    message.Reset();
                    yield return operationResult;
                }

                buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            } while (position != null);

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync().ConfigureAwait(false);
    }

    private static void WriteToMessage(ArrayWriter message, ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            var size = buffer.First.Span.Length;

            if (size > 0)
            {
                var span = message.GetSpan(size);
                buffer.First.Span.CopyTo(span);
                message.Advance(size);
            }
        }
        else
        {
            foreach (var segment in buffer)
            {
                var size = segment.Span.Length;
                var span = message.GetSpan(size);
                segment.Span.CopyTo(span);
                message.Advance(size);
            }
        }

        var lineEnd = message.GetSpan(1);
        lineEnd[0] = (byte) '\n';
        message.Advance(1);
    }

    private static bool IsMessageComplete(ArrayWriter message)
    {
        var length = message.Length;

        if (length < 2)
        {
            return false;
        }

        if (length == 2)
        {
            var span = message.GetWrittenSpan().Slice(0, 2);

            if (span[0] == (byte) '\n' && span[1] == (byte) '\n')
            {
                message.Reset();
            }
            return false;
        }

        if (length == 3)
        {
            var span = message.GetWrittenSpan().Slice(0, 3);

            if (span[0] == (byte) ':' && span[1] == (byte) '\n' && span[2] == (byte) '\n')
            {
                message.Reset();
                return false;
            }

            if (span[1] == (byte) '\n' && span[2] == (byte) '\n')
            {
                return true;
            }

            return false;
        }

        var last = message.GetWrittenSpan().Slice(length - 2, 2);

        if (last[0] == (byte) '\n' && last[1] == (byte) '\n')
        {
            return true;
        }

        return false;
    }

    private static bool TryReadMessage(
        ReadOnlyMemory<byte> message,
        [NotNullWhen(true)] out OperationResult? result)
    {
        var span = message.Span;
        var type = ParseEventType(ref span);

        if(type is EventType.Next)
        {
            result = ParseData(ref span);
            return true;
        }

        result = null;
        return false;
    }

    private static EventType ParseEventType(ref ReadOnlySpan<byte> span)
    {
        if (ExpectEvent(ref span))
        {
            if (ExpectNext(ref span))
            {
                return EventType.Next;
            }

            if (ExpectComplete(ref span))
            {
                return EventType.Complete;
            }
        }

        throw new InvalidOperationException("Invalid Message Format.");
    }

    private static OperationResult ParseData(ref ReadOnlySpan<byte> span)
    {
        if (ExpectData(ref span))
        {
            var data = ReadData(ref span);
            return OperationResult.Parse(data);
        }

        throw new InvalidOperationException("Invalid Message Format.");
    }

    private static bool ExpectEvent(ref ReadOnlySpan<byte> span)
    {
        if (span.Slice(0, 6).SequenceEqual(Event))
        {
            span = span.Slice(6);
            return true;
        }

        return false;
    }

    private static bool ExpectNext(ref ReadOnlySpan<byte> span)
    {
        SkipWhitespaces(ref span);

        if (!span.Slice(0, 5).SequenceEqual(Next))
        {
            return false;
        }

        span = span.Slice(5);

        return true;
    }

    private static bool ExpectComplete(ref ReadOnlySpan<byte> span)
    {
        SkipWhitespaces(ref span);

        if (!span.Slice(0, 9).SequenceEqual(Complete))
        {
            return false;
        }

        span = span.Slice(9);

        return true;
    }

    private static bool ExpectData(ref ReadOnlySpan<byte> span)
    {
        if (span.Slice(0, 5).SequenceEqual(Data))
        {
            span = span.Slice(5);
            return true;
        }

        return false;
    }

    private static ReadOnlySpan<byte> ReadData(ref ReadOnlySpan<byte> span)
    {
        var linebreak = span.IndexOf((byte) '\n');

        if (linebreak == -1)
        {
            throw new InvalidOperationException("Invalid Message Format.");
        }

        var data = span.Slice(0, linebreak);
        span = span.Slice(linebreak + 1);
        return data;
    }

    private static void SkipWhitespaces(ref ReadOnlySpan<byte> span)
    {
        while (span.Length > 0 && span[0] == (byte)' ')
        {
            span = span.Slice(1);
        }
    }

    private enum EventType
    {
        Next,
        Complete,
    }
}

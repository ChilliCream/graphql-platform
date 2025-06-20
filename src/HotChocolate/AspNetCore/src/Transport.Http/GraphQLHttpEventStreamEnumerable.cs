using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http;

internal sealed class GraphQLHttpEventStreamEnumerable : IAsyncEnumerable<OperationResult>
{
    private readonly HttpResponseMessage _message;

    public GraphQLHttpEventStreamEnumerable(HttpResponseMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _message = message;
    }

    public IAsyncEnumerator<OperationResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new GraphQLHttpEventStreamEnumerator(_message, cancellationToken);
}

internal class GraphQLHttpEventStreamEnumerator : IAsyncEnumerator<OperationResult>
{
    private static ReadOnlySpan<byte> Event => "event:"u8;
    private static ReadOnlySpan<byte> Next => "next\n"u8;
    private static ReadOnlySpan<byte> Complete => "complete\n"u8;
    private static ReadOnlySpan<byte> Data => "data:"u8;

    private readonly PooledArrayWriter _messageBuffer = new();
    private readonly CancellationTokenSource _cts;
    private readonly PipeReader _reader;
    private readonly PipeWriter _writer;
    private readonly CancellationToken _ct;
    private readonly HttpResponseMessage _message;
    private Stream? _contentStream;

    private bool _disposed;

    public GraphQLHttpEventStreamEnumerator(HttpResponseMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        _message = message;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ct = _cts.Token;

        var pipe = new Pipe();
        _reader = pipe.Reader;
        _writer = pipe.Writer;
    }

    public OperationResult Current { get; private set; } = null!;

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_contentStream is null)
        {
            var stream = _contentStream = await _message.Content.ReadAsStreamAsync(_ct).ConfigureAwait(false);

            var sourceEncoding = HttpTransportUtilities.GetEncoding(_message.Content.Headers.ContentType?.CharSet);
            if (HttpTransportUtilities.NeedsTranscoding(sourceEncoding))
            {
                stream = HttpTransportUtilities.GetTranscodingStream(stream, sourceEncoding);
            }

            ReadFromTransportAsync(stream, _writer, _ct).FireAndForget();
        }

        var result = await _reader.ReadAsync(_cts.Token).ConfigureAwait(false);
        if (result.IsCanceled)
        {
            return false;
        }

        var buffer = result.Buffer;
        SequencePosition? position;

        do
        {
            position = buffer.PositionOf((byte)'\n');
            if (position is null)
            {
                if (result.IsCompleted)
                {
                    // no more data to read
                    return false;
                }

                continue;
            }

            WriteLineToMessage(_messageBuffer, buffer.Slice(0, position.Value));

            if (IsMessageComplete(_messageBuffer))
            {
                var readState = TryReadResult(_messageBuffer.GetWrittenSpan(), out var operationResult);

                if (readState is ReadState.Message)
                {
                    _messageBuffer.Reset();
                    Current = operationResult!;
                    _reader.AdvanceTo(buffer.GetPosition(1, position.Value), buffer.End);
                    return true;
                }

                await _reader.CompleteAsync().ConfigureAwait(false);
                if (readState is ReadState.Complete)
                {
                    // graceful completion
                    return false;
                }

                throw new InvalidOperationException("Malformed message received.");
            }

            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        } while (position is not null);

        return false;
    }

    private static void WriteLineToMessage(PooledArrayWriter message, ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            var span = buffer.First.Span;
            if (span.Length > 0 && span[^1] == (byte)'\r')
            {
                span = span[..^1];
            }
            span.CopyTo(message.GetSpan(span.Length));
            message.Advance(span.Length);
        }
        else
        {
            foreach (var segment in buffer)
            {
                var spanSegment = segment.Span;
                spanSegment.CopyTo(message.GetSpan(spanSegment.Length));
                message.Advance(spanSegment.Length);
            }
        }

        // re-add unified line break (LF only)
        message.GetSpan(1)[0] = (byte)'\n';
        message.Advance(1);
    }

    private static bool IsMessageComplete(PooledArrayWriter message)
    {
        var span = message.GetWrittenSpan();
        return span.Length >= 2 && span[^1] == (byte)'\n' && span[^2] == (byte)'\n';
    }

    private static ReadState TryReadResult(ReadOnlySpan<byte> message, out OperationResult? result)
    {
        var type = ParseEventType(ref message);
        if (type is EventType.Next)
        {
            result = ParseData(ref message);
            return ReadState.Message;
        }
        if (type is EventType.Complete)
        {
            result = null;
            return ReadState.Complete;
        }
        result = null;
        return ReadState.Malformed;
    }

    private static EventType ParseEventType(ref ReadOnlySpan<byte> span)
    {
        var debug = Encoding.UTF8.GetString(span);

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

    /// <summary>
    /// Collects <c>data:</c> lines until the blank-line separator and concatenates them with LF.
    /// </summary>
    private static OperationResult ParseData(ref ReadOnlySpan<byte> span)
    {
        if (!ExpectData(ref span))
        {
            throw new InvalidOperationException("Invalid Message Format.");
        }

        SkipWhitespaces(ref span);

        using var payload = new PooledArrayWriter();

        while (true)
        {
            // read one logical line up to LF or end
            var lineBreak = span.IndexOf((byte)'\n');
            ReadOnlySpan<byte> line;
            if (lineBreak == -1)
            {
                line = span;
                span = default;
            }
            else
            {
                line = span[..lineBreak];
                span = span[(lineBreak + 1)..];
            }

            // Remove optional leading space
            SkipWhitespaces(ref line);

            // append to buffer (insert LF between lines)
            if (payload.Length > 0)
            {
                payload.GetSpan(1)[0] = (byte)'\n';
                payload.Advance(1);
            }
            line.CopyTo(payload.GetSpan(line.Length));
            payload.Advance(line.Length);

            // if the next part does not start with another data: line we are done
            if (span.Length < 5 || !span.StartsWith(Data))
            {
                break;
            }

            // consume next "data:" token & following whitespace
            span = span[5..];
            SkipWhitespaces(ref span);
        }

        return OperationResult.Parse(payload.GetWrittenSpan());
    }

    private static async Task ReadFromTransportAsync(Stream stream, PipeWriter writer, CancellationToken ct)
    {
        const int bufferSize = 4096;
        var buffer = new byte[bufferSize];
        var bufferMemory = new Memory<byte>(buffer);
        var temp = new PooledArrayWriter();

        await using var tokenRegistration = ct.Register(
            static w => ((PipeWriter)w!).CancelPendingFlush(),
            writer,
            useSynchronizationContext: false);

        while (true)
        {
            int bytesRead;
            try
            {
                bytesRead = await stream.ReadAsync(bufferMemory, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await writer.CompleteAsync(ex).ConfigureAwait(false);
                return;
            }

            if (bytesRead == 0)
            {
                break;
            }

            var span = buffer.AsSpan(0, bytesRead);
            NormalizeLineEndings(ref span);
            WriteToPipe(writer, span);
            temp.Write(span);

            var result = await writer.FlushAsync(ct).ConfigureAwait(false);
            if (result.IsCompleted || result.IsCanceled)
            {
                break;
            }
        }

        var debug = Encoding.UTF8.GetString(temp.GetWrittenSpan());

        await writer.CompleteAsync().ConfigureAwait(false);
    }

    private static void NormalizeLineEndings(ref Span<byte> span)
    {
        var normalized = span;

        var j = 0;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] != (byte)'\r')
            {
                normalized[j++] = span[i];
            }
        }

        span = normalized[..j];
    }

    private static void WriteToPipe(PipeWriter writer, ReadOnlySpan<byte> message)
    {
        message.CopyTo(writer.GetSpan(message.Length));
        writer.Advance(message.Length);
    }

    #region Expect helpers

    private static bool ExpectEvent(ref ReadOnlySpan<byte> span)
        => ConsumeToken(ref span, Event);

    private static bool ExpectNext(ref ReadOnlySpan<byte> span)
        => ConsumeTokenWithOptionalWhitespace(ref span, Next);

    private static bool ExpectComplete(ref ReadOnlySpan<byte> span)
        => ConsumeTokenWithOptionalWhitespace(ref span, Complete);

    private static bool ExpectData(ref ReadOnlySpan<byte> span)
        => ConsumeToken(ref span, Data);

    private static bool ConsumeToken(ref ReadOnlySpan<byte> span, ReadOnlySpan<byte> token)
    {
        if (span.Length < token.Length || !span.StartsWith(token))
        {
            return false;
        }
        span = span[token.Length..];
        return true;
    }

    private static bool ConsumeTokenWithOptionalWhitespace(ref ReadOnlySpan<byte> span, ReadOnlySpan<byte> token)
    {
        SkipWhitespaces(ref span);
        return ConsumeToken(ref span, token);
    }

    #endregion

    private static void SkipWhitespaces(ref ReadOnlySpan<byte> span)
    {
        while (span.Length > 0 && IsWhitespace(span[0]))
        {
            span = span[1..];
        }
    }

    private static bool IsWhitespace(byte b) => b is (byte)' ' or (byte)'\t';

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _cts.CancelAsync();
        await _reader.CompleteAsync().ConfigureAwait(false);
        await _writer.CompleteAsync().ConfigureAwait(false);

        if (_contentStream is not null)
        {
            await _contentStream.DisposeAsync().ConfigureAwait(false);
        }

        _message.Dispose();
        _messageBuffer.Dispose();
        _cts.Dispose();
    }

    private enum EventType
    {
        Next,
        Complete
    }

    private enum ReadState
    {
        Message,
        Complete,
        Malformed
    }
}

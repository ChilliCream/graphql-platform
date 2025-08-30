using System.Runtime.CompilerServices;
using HotChocolate.Buffers;

namespace HotChocolate.Transport.Http;

internal static class SseEventParser
{
    private static ReadOnlySpan<byte> Event => "event:"u8;
    private static ReadOnlySpan<byte> Data => "data:"u8;
    private static ReadOnlySpan<byte> NextEvent => "next"u8;
    private static ReadOnlySpan<byte> CompleteEvent => "complete"u8;

    public static SseEventData Parse(ReadOnlySpan<byte> message)
    {
        var type = ParseEventType(ref message);

        switch (type)
        {
            case SseEventType.Next:
                var buffer = ParseData(ref message);
                return new SseEventData(SseEventType.Next, buffer);

            case SseEventType.Complete:
                return new SseEventData(SseEventType.Complete, null);

            default:
                return new SseEventData(SseEventType.Unknown, null);
        }
    }

    /// <summary>
    /// Collects <c>data:</c> lines until the blank-line separator and concatenates them with LF.
    /// </summary>
    private static PooledArrayWriter ParseData(ref ReadOnlySpan<byte> span)
    {
        if (span.Length < Data.Length || !span.StartsWith(Data))
        {
            throw new GraphQLHttpStreamException("Invalid GraphQL over SSE Message Format.");
        }

        var payload = new PooledArrayWriter();

        try
        {
            while (ConsumeToken(ref span, Data))
            {
                SkipWhitespaces(ref span);

                // read one logical line up to LF or end
                var lineBreak = span.IndexOf((byte)'\n');
                ReadOnlySpan<byte> line;
                switch (lineBreak)
                {
                    case -1:
                        line = span;
                        span = default;
                        break;
                    case > 0 when span[lineBreak - 1] == (byte)'\r':
                        line = span[..(lineBreak - 1)];
                        span = span[(lineBreak + 1)..];
                        break;
                    default:
                        line = span[..lineBreak];
                        span = span[(lineBreak + 1)..];
                        break;
                }

                if (line.Length > 0)
                {
                    // Remove optional leading space
                    SkipWhitespaces(ref line);
                }

                // append to buffer (insert LF between lines)
                if (payload.Length > 0)
                {
                    payload.GetSpan(1)[0] = (byte)'\n';
                    payload.Advance(1);
                }

                if (line.Length > 0)
                {
                    line.CopyTo(payload.GetSpan(line.Length));
                    payload.Advance(line.Length);
                }
            }

            return payload;
        }
        catch
        {
            payload.Dispose();
            throw;
        }
    }

    private static SseEventType ParseEventType(ref ReadOnlySpan<byte> span)
    {
        if (ExpectEvent(ref span))
        {
            if (ExpectNext(ref span))
            {
                return SseEventType.Next;
            }

            if (ExpectComplete(ref span))
            {
                return SseEventType.Complete;
            }
        }

        return SseEventType.Unknown;
    }

    private static bool ExpectEvent(ref ReadOnlySpan<byte> span)
        => ConsumeToken(ref span, Event);

    private static bool ExpectNext(ref ReadOnlySpan<byte> span)
        => ConsumeTokenWithOptionalWhitespace(ref span, NextEvent);

    private static bool ExpectComplete(ref ReadOnlySpan<byte> span)
        => ConsumeTokenWithOptionalWhitespace(ref span, CompleteEvent);

    private static bool ConsumeTokenWithOptionalWhitespace(ref ReadOnlySpan<byte> span, ReadOnlySpan<byte> token)
    {
        SkipWhitespaces(ref span);

        if (ConsumeToken(ref span, token))
        {
            SkipNewLine(ref span);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ConsumeToken(ref ReadOnlySpan<byte> span, ReadOnlySpan<byte> token)
    {
        if (span.Length < token.Length || !span.StartsWith(token))
        {
            return false;
        }

        span = span[token.Length..];
        return true;
    }

    private static void SkipWhitespaces(ref ReadOnlySpan<byte> span)
    {
        while (span.Length > 0 && IsWhitespace(span[0]))
        {
            span = span[1..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespace(byte b) => b is (byte)' ' or (byte)'\t';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SkipNewLine(ref ReadOnlySpan<byte> span)
    {
        if (span.Length > 0 && span[0] == (byte)'\r')
        {
            span = span[1..];
        }

        if (span.Length > 0 && span[0] == (byte)'\n')
        {
            span = span[1..];
        }
    }
}

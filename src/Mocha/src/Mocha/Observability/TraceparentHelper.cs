using System.Diagnostics;

namespace Mocha;

internal static class TraceparentHelper
{
    private const int TraceparentLength = 55;

    /// <summary>
    /// Formats a W3C traceparent header value from the given activity.
    /// Returns null if the activity has no valid trace or span ID.
    /// Format: "00-{traceId 32 hex}-{spanId 16 hex}-{flags 2 hex}"
    /// </summary>
    internal static string? FormatTraceparent(Activity activity)
    {
        var traceId = activity.TraceId;
        var spanId = activity.SpanId;

        if (traceId == default || spanId == default)
        {
            return null;
        }

        return FormatTraceparent(traceId, spanId, activity.ActivityTraceFlags);
    }

    internal static string FormatTraceparent(
        ActivityTraceId traceId,
        ActivitySpanId spanId,
        ActivityTraceFlags flags)
    {
        Span<byte> traceIdBytes = stackalloc byte[16];
        Span<byte> spanIdBytes = stackalloc byte[8];
        traceId.CopyTo(traceIdBytes);
        spanId.CopyTo(spanIdBytes);

        Span<char> buffer = stackalloc char[TraceparentLength];
        buffer[0] = '0';
        buffer[1] = '0';
        buffer[2] = '-';
        HexEncode(traceIdBytes, buffer[3..]);
        buffer[35] = '-';
        HexEncode(spanIdBytes, buffer[36..]);
        buffer[52] = '-';
        ((byte)flags).TryFormat(buffer[53..], out _, "x2");

        return new string(buffer);
    }

    private static void HexEncode(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            destination[i * 2] = ToHexChar(b >> 4);
            destination[i * 2 + 1] = ToHexChar(b & 0xF);
        }
    }

    private static char ToHexChar(int value)
        => (char)(value < 10 ? '0' + value : 'a' + value - 10);
}

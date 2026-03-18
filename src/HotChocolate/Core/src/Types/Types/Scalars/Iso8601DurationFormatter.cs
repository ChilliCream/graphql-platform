using System.Buffers.Text;

namespace HotChocolate.Types;

/// <summary>
/// Zero-allocation formatter that writes <see cref="TimeSpan"/> values
/// as ISO 8601 duration strings in UTF-8.
/// Output format: <c>[-]P[nD][T[nH][nM][n[.f]S]]</c>
/// </summary>
internal static class Iso8601DurationFormatter
{
    // Maximum output: "-P10675199DT23H59M59.9999999S" = 30 bytes.
    // 64 bytes provides a comfortable margin.
    private const int MaxBufferSize = 64;

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> as an ISO 8601 duration string into a UTF-8 byte span.
    /// </summary>
    public static bool TryFormat(TimeSpan value, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;

        // Zero is always represented as "PT0S".
        if (value == TimeSpan.Zero)
        {
            if (destination.Length < 4)
            {
                return false;
            }

            destination[0] = (byte)'P';
            destination[1] = (byte)'T';
            destination[2] = (byte)'0';
            destination[3] = (byte)'S';
            bytesWritten = 4;
            return true;
        }

        var ticks = value.Ticks;
        var isNegative = ticks < 0;

        // Compute unsigned absolute ticks to handle TimeSpan.MinValue correctly.
        ulong absTicks;
        unchecked
        {
            absTicks = isNegative ? (ulong)-ticks : (ulong)ticks;
        }

        // Decompose the absolute duration into components.
        var days = (int)(absTicks / TimeSpan.TicksPerDay);
        var remainingTicks = absTicks % TimeSpan.TicksPerDay;
        var hours = (int)(remainingTicks / TimeSpan.TicksPerHour);
        remainingTicks %= TimeSpan.TicksPerHour;
        var minutes = (int)(remainingTicks / TimeSpan.TicksPerMinute);
        remainingTicks %= TimeSpan.TicksPerMinute;
        var seconds = (int)(remainingTicks / TimeSpan.TicksPerSecond);
        var fracTicks = (int)(remainingTicks % TimeSpan.TicksPerSecond);

        var pos = 0;

        if (isNegative)
        {
            if (pos >= destination.Length)
            {
                return false;
            }

            destination[pos++] = (byte)'-';
        }

        if (pos >= destination.Length)
        {
            return false;
        }

        destination[pos++] = (byte)'P';

        if (days > 0)
        {
            if (!TryWriteInt(days, destination, ref pos))
            {
                return false;
            }

            if (pos >= destination.Length)
            {
                return false;
            }

            destination[pos++] = (byte)'D';
        }

        var hasTimePart = hours > 0 || minutes > 0 || seconds > 0 || fracTicks > 0;

        if (hasTimePart)
        {
            if (pos >= destination.Length)
            {
                return false;
            }

            destination[pos++] = (byte)'T';

            if (hours > 0)
            {
                if (!TryWriteInt(hours, destination, ref pos))
                {
                    return false;
                }

                if (pos >= destination.Length)
                {
                    return false;
                }

                destination[pos++] = (byte)'H';
            }

            if (minutes > 0)
            {
                if (!TryWriteInt(minutes, destination, ref pos))
                {
                    return false;
                }

                if (pos >= destination.Length)
                {
                    return false;
                }

                destination[pos++] = (byte)'M';
            }

            if (seconds > 0 || fracTicks > 0)
            {
                if (!TryWriteInt(seconds, destination, ref pos))
                {
                    return false;
                }

                if (fracTicks > 0)
                {
                    if (pos >= destination.Length)
                    {
                        return false;
                    }

                    destination[pos++] = (byte)'.';

                    if (!TryWriteFraction(fracTicks, destination, ref pos))
                    {
                        return false;
                    }
                }

                if (pos >= destination.Length)
                {
                    return false;
                }

                destination[pos++] = (byte)'S';
            }
        }

        bytesWritten = pos;
        return true;
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> as an ISO 8601 duration string.
    /// </summary>
    public static string Format(TimeSpan value)
    {
        Span<byte> buffer = stackalloc byte[MaxBufferSize];
        TryFormat(value, buffer, out var bytesWritten);

        // All output characters are ASCII, so direct byte-to-char widening is safe.
        Span<char> chars = stackalloc char[bytesWritten];
        for (var i = 0; i < bytesWritten; i++)
        {
            chars[i] = (char)buffer[i];
        }

        return new string(chars);
    }

    private static bool TryWriteInt(int value, Span<byte> destination, ref int pos)
    {
        if (!Utf8Formatter.TryFormat(value, destination[pos..], out var written))
        {
            return false;
        }

        pos += written;
        return true;
    }

    /// <summary>
    /// Writes a sub-second tick value (1..9999999) as fractional digits with trailing zeros trimmed.
    /// TimeSpan has tick precision (1 tick = 100ns), so this writes up to 7 decimal digits.
    /// </summary>
    private static bool TryWriteFraction(int fracTicks, Span<byte> destination, ref int pos)
    {
        // Decompose into 7 decimal digits (tick precision).
        Span<byte> digits = stackalloc byte[7];
        var n = fracTicks;
        for (var i = 6; i >= 0; i--)
        {
            digits[i] = (byte)('0' + n % 10);
            n /= 10;
        }

        // Trim trailing zeros for a compact representation.
        var len = 7;
        while (len > 0 && digits[len - 1] == (byte)'0')
        {
            len--;
        }

        if (pos + len > destination.Length)
        {
            return false;
        }

        digits[..len].CopyTo(destination[pos..]);
        pos += len;
        return true;
    }
}

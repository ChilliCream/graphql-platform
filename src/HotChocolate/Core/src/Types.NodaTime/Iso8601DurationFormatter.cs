using System.Buffers.Text;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Zero-allocation formatter that writes <see cref="Duration"/> values
/// as ISO 8601 duration strings in UTF-8.
/// Output format: <c>[-]P[nD][T[nH][nM][n[.f]S]]</c>
/// </summary>
internal static class Iso8601DurationFormatter
{
    // Maximum output: "-P2147483647DT23H59M59.999999999S" = 34 bytes.
    // 64 bytes provides a comfortable margin.
    private const int MaxBufferSize = 64;

    /// <summary>
    /// Formats a <see cref="Duration"/> as an ISO 8601 duration string into a UTF-8 byte span.
    /// </summary>
    public static bool TryFormat(Duration value, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;

        // Zero is always represented as "PT0S".
        if (value == Duration.Zero)
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

        var isNegative = value < Duration.Zero;
        if (isNegative)
        {
            value = -value;
        }

        // Decompose the absolute duration into components.
        var days = value.Days;
        var hours = value.Hours;
        var minutes = value.Minutes;
        var seconds = value.Seconds;
        var nanos = value.SubsecondNanoseconds;

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

        var hasTimePart = hours > 0 || minutes > 0 || seconds > 0 || nanos > 0;

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

            if (seconds > 0 || nanos > 0)
            {
                if (!TryWriteInt(seconds, destination, ref pos))
                {
                    return false;
                }

                if (nanos > 0)
                {
                    if (pos >= destination.Length)
                    {
                        return false;
                    }

                    destination[pos++] = (byte)'.';

                    if (!TryWriteFraction(nanos, destination, ref pos))
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
    /// Formats a <see cref="Duration"/> as an ISO 8601 duration string.
    /// </summary>
    public static string Format(Duration value)
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
    /// Writes a nanosecond value (1..999999999) as fractional digits with trailing zeros trimmed.
    /// </summary>
    private static bool TryWriteFraction(int nanos, Span<byte> destination, ref int pos)
    {
        // Decompose into 9 decimal digits.
        Span<byte> digits = stackalloc byte[9];
        var n = nanos;
        for (var i = 8; i >= 0; i--)
        {
            digits[i] = (byte)('0' + n % 10);
            n /= 10;
        }

        // Trim trailing zeros for a compact representation.
        var len = 9;
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

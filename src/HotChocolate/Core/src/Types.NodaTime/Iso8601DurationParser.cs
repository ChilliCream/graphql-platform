using System.Runtime.CompilerServices;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Zero-allocation parser for ISO 8601 duration strings.
/// Supports the full format including ISO 8601-2:2019 per-component sign extensions:
/// <c>[-]P[[-]nY][[-]nM][[-]nW][[-]nD][T[[-]nH][[-]nM][[-]n[.f]S]]</c>
/// </summary>
internal static class Iso8601DurationParser
{
    // Pre-computed unit durations to avoid repeated construction.
    private static readonly Duration s_oneYear = Duration.FromDays(365);
    private static readonly Duration s_oneMonth = Duration.FromDays(30);
    private static readonly Duration s_oneWeek = Duration.FromDays(7);
    private static readonly Duration s_oneDay = Duration.FromDays(1);
    private static readonly Duration s_oneHour = Duration.FromHours(1);

    /// <summary>
    /// Attempts to parse an ISO 8601 duration string from a UTF-16 character span.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> input, out Duration result)
        => TryParseCore<char, CharReader>(input, out result);

    /// <summary>
    /// Attempts to parse an ISO 8601 duration string from a UTF-8 byte span.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> input, out Duration result)
        => TryParseCore<byte, ByteReader>(input, out result);

    private static bool TryParseCore<T, TReader>(ReadOnlySpan<T> input, out Duration result)
        where T : struct, IEquatable<T>
        where TReader : struct, ICharReader<T>
    {
        result = default;

        if (input.IsEmpty)
        {
            return false;
        }

        var pos = 0;
        var overallNegative = false;

        // Optional leading '-' negates the entire duration.
        if (TReader.IsChar(input[pos], '-'))
        {
            overallNegative = true;
            pos++;
        }

        // The 'P' designator is required.
        if (pos >= input.Length || !TReader.IsChar(input[pos], 'P'))
        {
            return false;
        }

        pos++;

        // Must have at least one component or 'T' after 'P'.
        if (pos >= input.Length)
        {
            return false;
        }

        var accumulated = Duration.Zero;
        var hasAnyComponent = false;
        var inTimePart = false;

        try
        {
            while (pos < input.Length)
            {
                // 'T' designator switches from date to time components.
                if (!inTimePart && TReader.IsChar(input[pos], 'T'))
                {
                    inTimePart = true;
                    pos++;

                    // At least one time component must follow 'T'.
                    if (pos >= input.Length)
                    {
                        return false;
                    }

                    continue;
                }

                // Optional per-component '-' sign (ISO 8601-2:2019).
                var componentNegative = false;
                if (TReader.IsChar(input[pos], '-'))
                {
                    componentNegative = true;
                    pos++;
                }

                // At least one digit is required for the component value.
                if (pos >= input.Length || !TReader.IsDigit(input[pos]))
                {
                    return false;
                }

                // Parse the integer part of the component value.
                var integerPart = 0L;
                while (pos < input.Length && TReader.IsDigit(input[pos]))
                {
                    integerPart = integerPart * 10 + TReader.DigitValue(input[pos]);
                    pos++;

                    // Detect overflow (integerPart wraps to negative).
                    if (integerPart < 0)
                    {
                        return false;
                    }
                }

                // Parse optional fractional part (only valid on seconds).
                var fractionalNanos = 0L;
                var hasFraction = false;
                if (pos < input.Length
                    && (TReader.IsChar(input[pos], '.') || TReader.IsChar(input[pos], ',')))
                {
                    hasFraction = true;
                    pos++;

                    var fractionDigits = 0;
                    while (pos < input.Length && TReader.IsDigit(input[pos]) && fractionDigits < 9)
                    {
                        fractionalNanos = fractionalNanos * 10 + TReader.DigitValue(input[pos]);
                        fractionDigits++;
                        pos++;
                    }

                    // Discard excess fractional digits beyond nanosecond precision.
                    while (pos < input.Length && TReader.IsDigit(input[pos]))
                    {
                        pos++;
                    }

                    // Scale up to 9 decimal places (nanoseconds).
                    for (var i = fractionDigits; i < 9; i++)
                    {
                        fractionalNanos *= 10;
                    }
                }

                // A designator character must follow the number.
                if (pos >= input.Length)
                {
                    return false;
                }

                var designator = input[pos];
                pos++;

                // Fractional parts are only valid on the seconds ('S') designator.
                if (hasFraction && !(inTimePart && TReader.IsChar(designator, 'S')))
                {
                    return false;
                }

                Duration component;

                if (!inTimePart)
                {
                    if (TReader.IsChar(designator, 'Y'))
                    {
                        component = s_oneYear * integerPart;
                    }
                    else if (TReader.IsChar(designator, 'M'))
                    {
                        component = s_oneMonth * integerPart;
                    }
                    else if (TReader.IsChar(designator, 'W'))
                    {
                        component = s_oneWeek * integerPart;
                    }
                    else if (TReader.IsChar(designator, 'D'))
                    {
                        component = s_oneDay * integerPart;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (TReader.IsChar(designator, 'H'))
                    {
                        component = s_oneHour * integerPart;
                    }
                    else if (TReader.IsChar(designator, 'M'))
                    {
                        component = Duration.FromMinutes(integerPart);
                    }
                    else if (TReader.IsChar(designator, 'S'))
                    {
                        component = Duration.FromSeconds(integerPart)
                            + Duration.FromNanoseconds(fractionalNanos);
                    }
                    else
                    {
                        return false;
                    }
                }

                // Apply both per-component and overall signs immediately.
                if (componentNegative != overallNegative)
                {
                    component = -component;
                }

                accumulated += component;
                hasAnyComponent = true;
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        if (!hasAnyComponent)
        {
            return false;
        }

        result = accumulated;
        return true;
    }

    private interface ICharReader<T> where T : struct
    {
        static abstract bool IsChar(T value, char c);
        static abstract bool IsDigit(T value);
        static abstract int DigitValue(T value);
    }

    private readonly struct CharReader : ICharReader<char>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChar(char value, char c) => value == c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(char value) => (uint)(value - '0') <= 9;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DigitValue(char value) => value - '0';
    }

    private readonly struct ByteReader : ICharReader<byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChar(byte value, char c) => value == (byte)c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(byte value) => (uint)(value - (byte)'0') <= 9;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DigitValue(byte value) => value - (byte)'0';
    }
}

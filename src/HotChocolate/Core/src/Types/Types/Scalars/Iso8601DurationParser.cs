using System.Runtime.CompilerServices;

namespace HotChocolate.Types;

/// <summary>
/// Zero-allocation parser for ISO 8601 duration strings targeting <see cref="TimeSpan"/>.
/// Supports the full format including ISO 8601-2:2019 per-component sign extensions:
/// <c>[-]P[[-]nY][[-]nM][[-]nW][[-]nD][T[[-]nH][[-]nM][[-]n[.f]S]]</c>
/// </summary>
internal static class Iso8601DurationParser
{
    /// <summary>
    /// Attempts to parse an ISO 8601 duration string from a UTF-16 character span.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> input, out TimeSpan result)
        => TryParseCore<char, CharReader>(input, out result);

    /// <summary>
    /// Attempts to parse an ISO 8601 duration string from a UTF-8 byte span.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> input, out TimeSpan result)
        => TryParseCore<byte, ByteReader>(input, out result);

    private static bool TryParseCore<T, TReader>(ReadOnlySpan<T> input, out TimeSpan result)
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

        var totalTicks = 0L;
        var hasAnyComponent = false;
        var inTimePart = false;

        try
        {
            checked
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
                    var fractionalTicks = 0L;
                    var hasFraction = false;
                    if (pos < input.Length
                        && (TReader.IsChar(input[pos], '.') || TReader.IsChar(input[pos], ',')))
                    {
                        hasFraction = true;
                        pos++;

                        // Parse up to 7 fractional digits (tick precision = 100ns).
                        var fractionDigits = 0;
                        while (pos < input.Length && TReader.IsDigit(input[pos]) && fractionDigits < 7)
                        {
                            fractionalTicks = fractionalTicks * 10 + TReader.DigitValue(input[pos]);
                            fractionDigits++;
                            pos++;
                        }

                        // Discard excess fractional digits beyond tick precision.
                        while (pos < input.Length && TReader.IsDigit(input[pos]))
                        {
                            pos++;
                        }

                        // Scale up to 7 decimal places (ticks).
                        for (var i = fractionDigits; i < 7; i++)
                        {
                            fractionalTicks *= 10;
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

                    long componentTicks;

                    if (!inTimePart)
                    {
                        if (TReader.IsChar(designator, 'Y'))
                        {
                            componentTicks = integerPart * 365 * TimeSpan.TicksPerDay;
                        }
                        else if (TReader.IsChar(designator, 'M'))
                        {
                            componentTicks = integerPart * 30 * TimeSpan.TicksPerDay;
                        }
                        else if (TReader.IsChar(designator, 'W'))
                        {
                            componentTicks = integerPart * 7 * TimeSpan.TicksPerDay;
                        }
                        else if (TReader.IsChar(designator, 'D'))
                        {
                            componentTicks = integerPart * TimeSpan.TicksPerDay;
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
                            componentTicks = integerPart * TimeSpan.TicksPerHour;
                        }
                        else if (TReader.IsChar(designator, 'M'))
                        {
                            componentTicks = integerPart * TimeSpan.TicksPerMinute;
                        }
                        else if (TReader.IsChar(designator, 'S'))
                        {
                            componentTicks = integerPart * TimeSpan.TicksPerSecond
                                + fractionalTicks;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    // Apply both per-component and overall signs immediately.
                    // This keeps the running total closer to zero, avoiding
                    // overflow when accumulating TimeSpan.MinValue's absolute ticks.
                    if (componentNegative != overallNegative)
                    {
                        componentTicks = -componentTicks;
                    }

                    totalTicks += componentTicks;
                    hasAnyComponent = true;
                }
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

        result = new TimeSpan(totalTicks);
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

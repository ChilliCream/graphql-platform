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

        // Track which components we've seen to enforce order and uniqueness.
        // Date components: Y=1, M=2, W=4, D=8
        // Time components: H=16, M=32, S=64
        var seenComponents = 0;

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

                    // At least one fractional digit is required after the decimal separator.
                    if (pos >= input.Length || !TReader.IsDigit(input[pos]))
                    {
                        return false;
                    }

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
                    // Use power-of-10 lookup for better performance.
                    if (fractionDigits < 9)
                    {
                        var multiplier = fractionDigits switch
                        {
                            0 => 1_000_000_000L,
                            1 => 100_000_000L,
                            2 => 10_000_000L,
                            3 => 1_000_000L,
                            4 => 100_000L,
                            5 => 10_000L,
                            6 => 1_000L,
                            7 => 100L,
                            8 => 10L,
                            _ => 1L
                        };
                        fractionalNanos *= multiplier;
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

                // Apply sign to the integer value before constructing the Duration to avoid overflow.
                var shouldNegate = componentNegative != overallNegative;
                var signedIntegerPart = shouldNegate ? -integerPart : integerPart;
                var signedFractionalNanos = shouldNegate ? -fractionalNanos : fractionalNanos;

                Duration component;
                int componentMask;

                if (!inTimePart)
                {
                    if (TReader.IsChar(designator, 'Y'))
                    {
                        componentMask = 1;
                        component = Duration.FromDays(signedIntegerPart * 365);
                    }
                    else if (TReader.IsChar(designator, 'M'))
                    {
                        componentMask = 2;
                        component = Duration.FromDays(signedIntegerPart * 30);
                    }
                    else if (TReader.IsChar(designator, 'W'))
                    {
                        componentMask = 4;
                        component = Duration.FromDays(signedIntegerPart * 7);
                    }
                    else if (TReader.IsChar(designator, 'D'))
                    {
                        componentMask = 8;
                        component = Duration.FromDays((int)signedIntegerPart);
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
                        componentMask = 16;
                        component = Duration.FromHours(signedIntegerPart);
                    }
                    else if (TReader.IsChar(designator, 'M'))
                    {
                        componentMask = 32;
                        component = Duration.FromMinutes(signedIntegerPart);
                    }
                    else if (TReader.IsChar(designator, 'S'))
                    {
                        componentMask = 64;
                        component = Duration.FromSeconds(signedIntegerPart)
                            + Duration.FromNanoseconds(signedFractionalNanos);
                    }
                    else
                    {
                        return false;
                    }
                }

                // Check for duplicate or out-of-order components.
                // Components must appear in order: Y M W D (H M S)
                // Duplicate: component bit is already set.
                if ((seenComponents & componentMask) != 0)
                {
                    return false;
                }

                // Out of order: any higher-order bit is set (components after this one).
                // Since masks are powers of 2 in ascending order (1,2,4,8,16,32,64),
                // checking if seenComponents >= componentMask catches out-of-order cases.
                if (seenComponents >= componentMask)
                {
                    return false;
                }

                seenComponents |= componentMask;

                accumulated += component;
                hasAnyComponent = true;
            }
        }
        catch (OverflowException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
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

    private interface ICharReader<in T> where T : struct
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

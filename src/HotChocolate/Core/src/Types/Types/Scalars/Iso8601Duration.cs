namespace HotChocolate.Types;

/// <summary>
/// ISO 8601 Duration based on XsdDuration which include additional "Weeks" part
/// https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.Xml/src/System/Xml/Schema/XsdDuration.cs
/// </summary>
internal struct Iso8601Duration
{
    [Flags]
    private enum Parts
    {
        HasNone = 0,
        HasYears = 1,
        HasMonths = 2,
        HasWeeks = 4,
        HasDays = 8,
        HasHours = 16,
        HasMinutes = 32,
        HasSeconds = 64,
    }

    /// <summary>
    /// Internal helper method that converts to a TimeSpan value. This code uses the estimate
    /// that there are 365 days in the year, 52 weeks in a year and 30 days in a month.
    /// </summary>
    internal static bool TryToTimeSpan(
        int years,
        int months,
        int weeks,
        int days,
        int hours,
        int minutes,
        int seconds,
        uint nanoseconds,
        bool isNegative,
        out TimeSpan? result)
    {
        ulong ticks = 0;

        // Throw error if result cannot fit into a long
        try
        {
            checked
            {
                if (years != 0 && months != 0)
                {
                    ticks += ((ulong)years + (ulong)months / 12) * 365;
                }

                if (months != 0)
                {
                    ticks += ((ulong)months % 12) * 30;
                }

                if (weeks != 0)
                {
                    ticks += ((ulong)weeks % 52) * 7;
                }

                if (days != 0 || hours != 0 || minutes != 0 || seconds != 0 || nanoseconds != 0)
                {
                    ticks += (ulong)days;

                    ticks *= 24;
                    ticks += (ulong)hours;

                    ticks *= 60;
                    ticks += (ulong)minutes;

                    ticks *= 60;
                    ticks += (ulong)seconds;

                    // Tick count interval is in 100 nanosecond intervals (7 digits)
                    ticks *= TimeSpan.TicksPerSecond;
                    ticks += (ulong)nanoseconds / 100;
                }
                else
                {
                    // Multiply YearMonth duration by number of ticks per day
                    ticks *= TimeSpan.TicksPerDay;
                }

                if (isNegative)
                {
                    // Handle special case of Int64.MaxValue + 1 before negation,
                    // since it would otherwise overflow
                    result = ticks >= (ulong)long.MaxValue + 1
                        ? new TimeSpan(long.MinValue)
                        : new TimeSpan(-((long)ticks));
                }
                else
                {
                    result = new TimeSpan((long)ticks);
                }
                return true;
            }
        }
        catch (OverflowException)
        {
            result = TimeSpan.MinValue;
            return false;
        }
    }

    internal static bool TryParse(string s, out TimeSpan? result)
    {
        int years = default;
        int months = default;
        int weeks = default;
        int days = default;
        int hours = default;
        int minutes = default;
        int seconds = default;
        uint nanoseconds = default;
        var isNegative = false;

        var parts = Parts.HasNone;

        result = default;

        s = s.Trim();
        var length = s.Length;

        var pos = 0;

        if (pos >= length)
        {
            return false;
        }

        if (s[pos] == '-')
        {
            pos++;
            isNegative = true;
        }
        else
        {
            nanoseconds = 0;
        }

        if (pos >= length)
        {
            return false;
        }

        if (s[pos++] != 'P')
        {
            return false;
        }

        if (!TryParseDigits(s, ref pos, false, out var value, out var numDigits))
        {
            return false;
        }

        if (pos >= length)
        {
            return false;
        }

        if (s[pos] == 'Y')
        {
            if (numDigits == 0)
            {
                return false;
            }

            parts |= Parts.HasYears;
            years = value;
            if (++pos <= length)
            {
                if (!TryParseDigits(s, ref pos, false, out value, out numDigits))
                {
                    return false;
                }
            }
        }

        if (pos < length && s[pos] == 'M')
        {
            if (numDigits == 0)
            {
                return false;
            }

            parts |= Parts.HasMonths;
            months = value;
            if (++pos <= length)
            {
                if (!TryParseDigits(s, ref pos, false, out value, out numDigits))
                {
                    return false;
                }
            }
        }

        if (pos < length && s[pos] == 'W')
        {
            if (numDigits == 0)
            {
                return false;
            }

            parts |= Parts.HasWeeks;
            weeks = value;
            if (++pos <= length)
            {
                if (!TryParseDigits(s, ref pos, false, out value, out numDigits))
                {
                    return false;
                }
            }
        }

        if (pos < length && s[pos] == 'D')
        {
            if (numDigits == 0)
            {
                return false;
            }

            parts |= Parts.HasDays;
            days = value;
            if (++pos <= length)
            {
                if (!TryParseDigits(s, ref pos, false, out value, out numDigits))
                {
                    return false;
                }
            }
        }

        if (pos < length && s[pos] == 'T')
        {
            if (numDigits != 0)
            {
                return false;
            }

            pos++;
            if (!TryParseDigits(s, ref pos, false, out value, out numDigits))
            {
                return false;
            }

            if (pos >= length)
            {
                return false;
            }

            if (s[pos] == 'H')
            {
                if (numDigits == 0)
                {
                    return false;
                }

                parts |= Parts.HasHours;
                hours = value;
                if (++pos <= length)
                {
                    if (!TryParseDigits(s, ref pos, false, out value, out numDigits))
                    {
                        return false;
                    }
                }
            }

            if (pos < length && s[pos] == 'M')
            {
                if (numDigits == 0)
                {
                    return false;
                }

                parts |= Parts.HasMinutes;
                minutes = value;
                if (++pos <= length && !TryParseDigits(s, ref pos, false, out value, out numDigits))
                {
                    return false;
                }
            }

            if (pos < length && s[pos] == '.')
            {
                pos++;

                parts |= Parts.HasSeconds;
                seconds = value;

                if (!TryParseDigits(s, ref pos, true, out value, out numDigits))
                {
                    return false;
                }

                if (numDigits == 0)
                { //If there are no digits after the decimal point, assume 0
                    value = 0;
                }
                // Normalize to nanosecond intervals
                for (; numDigits > 9; numDigits--)
                {
                    value /= 10;
                }

                for (; numDigits < 9; numDigits++)
                {
                    value *= 10;
                }

                nanoseconds |= (uint)value;

                if (pos >= length)
                {
                    return false;
                }

                if (s[pos] != 'S')
                {
                    return false;
                }
                pos++;
            }
            else if (pos < length && s[pos] == 'S')
            {
                if (numDigits == 0)
                {
                    return false;
                }

                parts |= Parts.HasSeconds;
                seconds = value;
                pos++;
            }
        }

        if (pos == length)
        {
            // At least one part must be defined
            if (parts == Parts.HasNone)
            {
                return false;
            }

            return TryToTimeSpan(years,
                months,
                weeks,
                days,
                hours,
                minutes,
                seconds,
                nanoseconds,
                isNegative,
                out result);
        }

        return false;
    }

    // Helper method that constructs an integer from leading digits starting at s[offset].
    // "offset" is updated to contain an offset just beyond the last digit.
    // The number of digits consumed is returned in cntDigits.
    // The integer is returned (0 if no digits).  If the digits cannot fit into an Int32:
    //   1. If eatDigits is true, then additional digits will be silently discarded
    //      (don't count towards numDigits)
    //   2. If eatDigits is false, an overflow exception is thrown
    private static bool TryParseDigits(string s, ref int offset, bool eatDigits, out int result, out int numDigits)
    {
        var offsetStart = offset;
        var offsetEnd = s.Length;

        result = 0;
        numDigits = 0;

        while (offset < offsetEnd && s[offset] >= '0' && s[offset] <= '9')
        {
            var digit = s[offset] - '0';

            if (result > (int.MaxValue - digit) / 10)
            {
                if (!eatDigits)
                {
                    return false;
                }

                // Skip past any remaining digits
                numDigits = offset - offsetStart;

                while (offset < offsetEnd && s[offset] >= '0' && s[offset] <= '9')
                {
                    offset++;
                }

                return true;
            }

            result = result * 10 + digit;
            offset++;
        }

        numDigits = offset - offsetStart;
        return true;
    }
}

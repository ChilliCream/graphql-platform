namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal static class TelemetryTimestamp
{
    public static readonly TimeSpan DefaultSince = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan MaximumAge = TimeSpan.FromDays(60);

    public static bool TryParse(
        string value,
        DateTimeOffset now,
        string optionName,
        bool enforceMaximumAge,
        out DateTimeOffset timestamp,
        out string? error)
    {
        if (TryParseDuration(value, out var duration))
        {
            if ((enforceMaximumAge && duration > MaximumAge)
                || duration > now - DateTimeOffset.MinValue)
            {
                timestamp = default;
                error = MaximumAgeMessage(optionName);
                return false;
            }

            try
            {
                timestamp = now - duration;
            }
            catch (ArgumentOutOfRangeException)
            {
                timestamp = default;
                error = MaximumAgeMessage(optionName);
                return false;
            }
        }
        else if (value.Contains('T')
            && DateTimeOffset.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal
                    | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out timestamp))
        {
            timestamp = timestamp.ToUniversalTime();
        }
        else
        {
            timestamp = default;
            error = InvalidValueMessage(optionName, value);
            return false;
        }

        if (enforceMaximumAge && now - timestamp > MaximumAge)
        {
            error = MaximumAgeMessage(optionName);
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryParseDuration(string value, out TimeSpan duration)
    {
        duration = default;

        if (value.Length < 2)
        {
            return false;
        }

        var unit = value[^1];
        var maximumValue = unit switch
        {
            's' => (long)TimeSpan.MaxValue.TotalSeconds,
            'm' => (long)TimeSpan.MaxValue.TotalMinutes,
            'h' => (long)TimeSpan.MaxValue.TotalHours,
            'd' => (long)TimeSpan.MaxValue.TotalDays,
            _ => -1
        };

        if (maximumValue < 0
            || !long.TryParse(
                value[..^1],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var valuePart)
            || valuePart > maximumValue)
        {
            return false;
        }

        switch (unit)
        {
            case 's':
                duration = TimeSpan.FromSeconds(valuePart);
                break;
            case 'm':
                duration = TimeSpan.FromMinutes(valuePart);
                break;
            case 'h':
                duration = TimeSpan.FromHours(valuePart);
                break;
            case 'd':
                duration = TimeSpan.FromDays(valuePart);
                break;
            default:
                return false;
        }

        return true;
    }

    private static string MaximumAgeMessage(string optionName)
        => $"Option '{optionName}' cannot be more than 60 days in the past."
            + Environment.NewLine
            + "hint: choose a more recent timestamp or duration.";

    private static string InvalidValueMessage(string optionName, string value)
        => $"Option '{optionName}' received an invalid value: {value}"
            + Environment.NewLine
            + "hint: use a duration such as 30m, 2h, or 7d, or an ISO 8601 timestamp.";
}

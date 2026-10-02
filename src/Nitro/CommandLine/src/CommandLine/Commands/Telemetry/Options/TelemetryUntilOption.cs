using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryUntilOption : Option<DateTimeOffset>
{
    public const string OptionName = "--until";

    public TelemetryUntilOption() : base(OptionName)
    {
        Description = "The latest timestamp to include [env: NITRO_UNTIL]";
        Required = false;
        DefaultValueFactory = result => GetDefaultTimestamp(result);
        CustomParser = result => ParseTimestamp(
            result.Tokens.Single().Value,
            result,
            TelemetryOptionDefaults.GetUtcNow());
    }

    private static DateTimeOffset GetDefaultTimestamp(ArgumentResult result)
    {
        var now = TelemetryOptionDefaults.GetUtcNow();
        var value = TelemetryOptionDefaults.GetEnvironmentValue(EnvironmentVariables.Until);

        return value is null
            ? now
            : ParseTimestamp(value, result, now);
    }

    private static DateTimeOffset ParseTimestamp(
        string value,
        ArgumentResult result,
        DateTimeOffset now)
    {
        if (TelemetryTimestamp.TryParse(
            value,
            now,
            OptionName,
            enforceMaximumAge: false,
            out var timestamp,
            out var error))
        {
            return timestamp;
        }

        result.AddError(error!);
        return default;
    }
}

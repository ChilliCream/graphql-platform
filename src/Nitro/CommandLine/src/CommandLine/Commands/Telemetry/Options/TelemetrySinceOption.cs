using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetrySinceOption : Option<DateTimeOffset>
{
    public const string OptionName = "--since";

    public TelemetrySinceOption() : base(OptionName)
    {
        Description = "The earliest timestamp to include [env: NITRO_SINCE]";
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
        var value = TelemetryOptionDefaults.GetEnvironmentValue(EnvironmentVariables.Since);

        return value is null
            ? now - TelemetryTimestamp.DefaultSince
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
            enforceMaximumAge: true,
            out var timestamp,
            out var error))
        {
            return timestamp;
        }

        result.AddError(error!);
        return default;
    }
}

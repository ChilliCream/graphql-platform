using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetrySinceOption : Option<DateTimeOffset>
{
    public const string OptionName = "--since";

    public TelemetrySinceOption() : base(OptionName)
    {
        Description = "The earliest timestamp to include";
        Required = false;
        DefaultValueFactory = _ => TelemetryOptionDefaults.GetDefaultSince();
        CustomParser = result =>
            ParseTimestamp(result.Tokens.Single().Value, result, TelemetryOptionDefaults.GetUtcNow());
    }

    private static DateTimeOffset ParseTimestamp(string value, ArgumentResult result, DateTimeOffset now)
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

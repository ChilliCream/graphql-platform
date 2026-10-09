using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryUntilOption : Option<DateTimeOffset>
{
    public const string OptionName = "--until";

    public TelemetryUntilOption() : base(OptionName)
    {
        Description = "The latest timestamp to include";
        Required = false;
        DefaultValueFactory = _ => TelemetryOptionDefaults.GetUtcNow();
        CustomParser = result =>
            ParseTimestamp(result.Tokens.Single().Value, result, TelemetryOptionDefaults.GetUtcNow());
    }

    private static DateTimeOffset ParseTimestamp(string value, ArgumentResult result, DateTimeOffset now)
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

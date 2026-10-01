using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryUntilOption : Option<DateTimeOffset>
{
    public const string OptionName = "--until";

    public TelemetryUntilOption() : base(OptionName)
    {
        Description = "The latest timestamp to include [env: NITRO_UNTIL]";
        Required = false;
        DefaultValueFactory = result => result.GetDefaultTimestamp();
        CustomParser = result => result.Tokens.Single().Value.ParseTimestamp(
            result,
            TelemetryOptionDefaults.GetUtcNow());
    }
}

file static class Extensions
{
    extension(ArgumentResult result)
    {
        public DateTimeOffset GetDefaultTimestamp()
        {
            var now = TelemetryOptionDefaults.GetUtcNow();
            var value = TelemetryOptionDefaults.GetEnvironmentValue(EnvironmentVariables.Until);

            return value is null
                ? now
                : value.ParseTimestamp(result, now);
        }
    }

    extension(string value)
    {
        public DateTimeOffset ParseTimestamp(ArgumentResult result, DateTimeOffset now)
        {
            if (TelemetryTimestamp.TryParse(
                value,
                now,
                TelemetryUntilOption.OptionName,
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
}

using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetrySinceOption : Option<DateTimeOffset>
{
    public const string OptionName = "--since";

    public TelemetrySinceOption() : base(OptionName)
    {
        Description = "The earliest timestamp to include [env: NITRO_SINCE]";
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
            var value = TelemetryOptionDefaults.GetEnvironmentValue(EnvironmentVariables.Since);

            return value is null
                ? now - TelemetryTimestamp.DefaultSince
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
                TelemetrySinceOption.OptionName,
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
}

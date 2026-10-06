namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal static class TelemetryOptionDefaults
{
    public static DateTimeOffset GetUtcNow()
        => CommandExecutionContext.Services.GetRequiredService<TimeProvider>().GetUtcNow();

    public static DateTimeOffset GetDefaultSince() => GetUtcNow() - TelemetryTimestamp.DefaultSince;
}

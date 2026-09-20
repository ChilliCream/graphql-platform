using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal static class TelemetryOptionDefaults
{
    public static string? GetEnvironmentValue(string name)
    {
        var provider = CommandExecutionContext.Services
            .GetRequiredService<IEnvironmentVariableProvider>();

        return provider.GetEnvironmentVariable($"NITRO_{name}")
            ?? provider.GetEnvironmentVariable($"BARISTA_{name}");
    }

    public static DateTimeOffset GetUtcNow()
        => CommandExecutionContext.Services
            .GetRequiredService<TimeProvider>()
            .GetUtcNow();
}

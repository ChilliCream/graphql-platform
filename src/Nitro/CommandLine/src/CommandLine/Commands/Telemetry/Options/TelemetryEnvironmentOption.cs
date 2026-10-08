namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryEnvironmentOption : Option<string[]>
{
    public TelemetryEnvironmentOption() : base("--env")
    {
        Description = "Limit results to an environment; can be used multiple times";
        Required = false;
        this.OneArgumentPerOccurrence();
        DefaultValueFactory = _ => [];
    }
}

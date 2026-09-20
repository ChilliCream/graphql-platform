namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryHasErrorOption : Option<bool>
{
    public TelemetryHasErrorOption() : base("--has-error")
    {
        Description = "Only include results with errors";
        Required = false;
    }
}

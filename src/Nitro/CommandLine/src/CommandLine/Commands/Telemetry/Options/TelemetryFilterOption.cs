namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryFilterOption : Option<string>
{
    public TelemetryFilterOption() : base("--filter")
    {
        Description = "Filter results using the telemetry filter grammar";
        Required = false;
        this.NonEmptyStringsOnly();
    }
}

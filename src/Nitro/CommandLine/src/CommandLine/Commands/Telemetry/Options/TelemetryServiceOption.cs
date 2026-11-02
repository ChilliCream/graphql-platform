namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryServiceOption : Option<string>
{
    public TelemetryServiceOption() : base("--service")
    {
        Description = "Limit results to a service";
        Required = false;
        this.NonEmptyStringsOnly();
    }
}

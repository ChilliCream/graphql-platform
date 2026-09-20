namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetrySearchOption : Option<string>
{
    public TelemetrySearchOption() : base("--search")
    {
        Description = "Search span names or log messages";
        Required = false;
        this.NonEmptyStringsOnly();
    }
}

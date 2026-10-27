namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal abstract class TelemetrySearchOption : Option<string>
{
    protected TelemetrySearchOption(string description) : base("--search")
    {
        Description = description;
        Required = false;
        this.NonEmptyStringsOnly();
    }
}

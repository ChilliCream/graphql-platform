namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetrySeverityOption : Option<TelemetrySeverity?>
{
    public TelemetrySeverityOption() : base("--severity")
    {
        Description = "Only include logs at or above this severity";
        Required = false;
    }
}

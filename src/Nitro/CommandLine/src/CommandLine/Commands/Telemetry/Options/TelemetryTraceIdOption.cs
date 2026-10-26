namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryTraceIdOption : Option<string>
{
    public TelemetryTraceIdOption() : base("--trace-id")
    {
        Description = "Only include logs from a trace";
        Required = false;
        this.NonEmptyStringsOnly();
    }
}

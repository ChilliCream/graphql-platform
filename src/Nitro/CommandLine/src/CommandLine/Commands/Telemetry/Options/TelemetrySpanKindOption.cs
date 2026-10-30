namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetrySpanKindOption : Option<TelemetrySpanKind[]>
{
    public TelemetrySpanKindOption() : base("--span-kind")
    {
        Description = "Limit results to a span kind; can be used multiple times";
        Required = false;
        this.OneArgumentPerOccurrence();
    }
}

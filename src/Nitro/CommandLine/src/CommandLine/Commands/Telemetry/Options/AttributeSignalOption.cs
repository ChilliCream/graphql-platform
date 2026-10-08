using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class AttributeSignalOption : Option<OpenTelemetrySignalKind>
{
    public AttributeSignalOption() : base("--signal")
    {
        Description = "The telemetry signal to inspect";
        Required = true;
        AcceptOnlyFromAmong("traces", "logs");
    }
}

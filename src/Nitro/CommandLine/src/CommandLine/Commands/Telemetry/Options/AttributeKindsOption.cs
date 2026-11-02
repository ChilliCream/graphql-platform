using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class AttributeKindsOption : Option<OpenTelemetryAttributeKind[]>
{
    public AttributeKindsOption() : base("--kind")
    {
        Description = "Limit results to an attribute kind; can be used multiple times";
        Required = false;
        this.OneArgumentPerOccurrence();
    }
}

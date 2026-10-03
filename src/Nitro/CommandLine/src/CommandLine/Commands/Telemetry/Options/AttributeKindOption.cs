using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class AttributeKindOption : Option<OpenTelemetryAttributeKind?>
{
    public AttributeKindOption() : base("--kind")
    {
        Description = "Limit results to an attribute kind";
        Required = false;
    }
}

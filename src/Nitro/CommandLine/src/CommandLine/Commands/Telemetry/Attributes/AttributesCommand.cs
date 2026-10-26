namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Attributes;

internal sealed class AttributesCommand : Command
{
    public AttributesCommand() : base("attributes")
    {
        Description = "Inspect telemetry attributes.";

        Subcommands.Add(new ListAttributeKeysCommand());
        Subcommands.Add(new ListAttributeValuesCommand());
    }
}

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.SourceSchema;

internal sealed class FusionSourceSchemaCommand : Command
{
    public FusionSourceSchemaCommand() : base("source-schema")
    {
        Description = "Manage source schemas.";

        Subcommands.Add(new FusionSourceSchemaInitCommand());
    }
}

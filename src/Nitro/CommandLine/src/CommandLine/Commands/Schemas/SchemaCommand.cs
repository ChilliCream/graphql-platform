namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class SchemaCommand : Command
{
    public SchemaCommand() : base("schema")
    {
        Description = "Manage schemas";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new PublishSchemaCommand());
        AddCommand(new ValidateSchemaCommand());
        AddCommand(new UploadSchemaCommand());
        AddCommand(new DownloadSchemaCommand());
    }
}

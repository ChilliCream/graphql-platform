namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class SchemaCommand : Command
{
    public SchemaCommand() : base("schema")
    {
        Description = "Upload, publish and validate schemas";

        AddCommand(new PublishSchemaCommand());
        AddCommand(new ValidateSchemaCommand());
        AddCommand(new UploadSchemaCommand());
        AddCommand(new DownloadSchemaCommand());
    }
}

namespace ChilliCream.Nitro.CLI.Option;

internal sealed class SchemaFileOption : Option<FileInfo>
{
    public SchemaFileOption() : base("--schema-file")
    {
        Description = "The path to the graphql file with the schema definition";
        IsRequired = true;
        this.DefaultFileFromEnvironmentValue("SCHEMA_FILE");
    }
}

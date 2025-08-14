namespace ChilliCream.Nitro.CLI.Option;

internal class BaseSchemaFileOption : Option<FileInfo>
{
    public BaseSchemaFileOption() : base("--schema")
    {
        Description = "The path to the graphql file with the schema";
        IsRequired = true;
        this.DefaultFileFromEnvironmentValue("SCHEMA_FILE");
    }
}

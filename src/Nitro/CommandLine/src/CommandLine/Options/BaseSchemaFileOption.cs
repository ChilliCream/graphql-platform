namespace ChilliCream.Nitro.CommandLine.Options;

internal class BaseSchemaFileOption : Option<string>
{
    public BaseSchemaFileOption() : base("--schema")
    {
        Description = "The path to the graphql file with the schema";
        Required = true;
        this.DefaultFileFromEnvironmentValue("SCHEMA_FILE");
        this.LegalFilePathsOnly();
    }
}

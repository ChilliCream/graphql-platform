namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class SchemaFileOption : Option<string>
{
    public SchemaFileOption() : base("--schema-file")
    {
        Description = "The path to the graphql file with the schema definition";
        Required = true;
        this.DefaultFileFromEnvironmentValue("SCHEMA_FILE");
        this.LegalFilePathsOnly();
    }
}

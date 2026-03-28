namespace ChilliCream.Nitro.CommandLine.Options;

internal class ExtensionFileOption : Option<string>
{
    public ExtensionFileOption() : base("--extension")
    {
        Description = "The path to the graphql file with the schema extension";
        Required = true;
        this.DefaultFileFromEnvironmentValue("SCHEMA_EXTENSION_FILE");
        this.LegalFilePathsOnly();
    }
}

namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal class ExtensionFileOption : Option<FileInfo>
{
    public ExtensionFileOption() : base("--extension")
    {
        Description = "The path to the graphql file with the schema extension";
        IsRequired = true;
        this.DefaultFileFromEnvironmentValue("SCHEMA_EXTENSION_FILE");
    }
}

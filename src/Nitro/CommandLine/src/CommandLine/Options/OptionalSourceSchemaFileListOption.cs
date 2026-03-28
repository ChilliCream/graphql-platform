namespace ChilliCream.Nitro.CommandLine.Options;

public sealed class OptionalSourceSchemaFileListOption : Option<List<string>>
{
    public const string OptionName = "--source-schema-file";

    public OptionalSourceSchemaFileListOption() : base(OptionName)
    {
        Description = "One or more paths to a source schema file (.graphqls) or directory containing a source schema file.";
        AddAlias("-f");
        this.LegalFilePathsOnly();
    }
}

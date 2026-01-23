namespace ChilliCream.Nitro.CommandLine.Options;

public sealed class SourceSchemaFileListOption : Option<List<string>>
{
    public SourceSchemaFileListOption() : this(false)
    {
    }

    public SourceSchemaFileListOption(bool isRequired) : base("--source-schema-file")
    {
        Description = "One or more paths to a source schema file (.graphqls) or directory containing a source schema file.";
        IsRequired = isRequired;
        AddAlias("-f");
        this.LegalFilePathsOnly();
    }
}

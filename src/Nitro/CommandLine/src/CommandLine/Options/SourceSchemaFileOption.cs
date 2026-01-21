namespace ChilliCream.Nitro.CommandLine.Options;

public sealed class SourceSchemaFileOption : Option<string>
{
    public SourceSchemaFileOption() : base("--source-schema-file")
    {
        Description = "The path to a source schema file (.graphqls) or directory containing a source schema file.";
        IsRequired = true;
        AddAlias("-f");
        this.LegalFilePathsOnly();
    }
}

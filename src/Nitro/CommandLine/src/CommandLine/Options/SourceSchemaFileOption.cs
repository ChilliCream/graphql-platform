namespace ChilliCream.Nitro.CommandLine;

public sealed class SourceSchemaFileOption : Option<string>
{
    public SourceSchemaFileOption() : base("--source-schema-file")
    {
        Description = "The path to a source schema file (.graphqls) or directory containing a source schema file";
        Required = true;
        Aliases.Add("-f");
        this.LegalFilePathsOnly();
    }
}

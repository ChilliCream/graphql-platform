namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSourceSchemaFileOption : Option<string>
{
    public const string OptionName = "--source-schema-file";

    public OptionalSourceSchemaFileOption() : base(OptionName)
    {
        Description =
            "The path to the source schema file (.graphqls) the settings belong to, "
            + "or a directory containing it";
        Aliases.Add("-f");
        this.LegalFilePathsOnly();
    }
}

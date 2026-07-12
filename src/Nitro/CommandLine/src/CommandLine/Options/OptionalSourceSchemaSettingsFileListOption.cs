namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSourceSchemaSettingsFileListOption : Option<List<string>>
{
    public const string OptionName = "--source-schema-settings-file";

    public OptionalSourceSchemaSettingsFileListOption()
        : base(OptionName)
    {
        Description = $"A settings file paired by occurrence with '{OptionalSourceSchemaUrlListOption.OptionName}'";
        Arity = ArgumentArity.ExactlyOne;
        AllowMultipleArgumentsPerToken = false;
        this.LegalFilePathsOnly();
    }
}

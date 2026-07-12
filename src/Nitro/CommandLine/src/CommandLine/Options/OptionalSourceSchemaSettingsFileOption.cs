namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSourceSchemaSettingsFileOption : Option<string>
{
    public const string OptionName = "--source-schema-settings-file";

    public OptionalSourceSchemaSettingsFileOption()
        : base(OptionName)
    {
        Description = $"A settings file paired by occurrence with '{OptionalSourceSchemaUrlOption.OptionName}'";
        Arity = ArgumentArity.ExactlyOne;
        AllowMultipleArgumentsPerToken = false;
        this.LegalFilePathsOnly();
    }
}

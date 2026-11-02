namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSettingsFileOption : Option<string>
{
    public const string OptionName = "--settings-file";

    public OptionalSettingsFileOption() : base(OptionName)
    {
        Description =
            "The path to write the settings file to, "
            + "instead of deriving it from the source schema file";
        this.LegalFilePathsOnly();
    }
}

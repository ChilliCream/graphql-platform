namespace ChilliCream.Nitro.CommandLine;

internal class FusionArchiveFileOption : Option<string>
{
    public const string OptionName = "--archive";

    public FusionArchiveFileOption() : base(OptionName)
    {
        Description = "The path to a Fusion archive file";
        Required = true;
        Aliases.Add("-a");
        this.DefaultFromEnvironmentValue(EnvironmentVariables.FusionConfigFile);
        this.LegalFilePathsOnly();
    }
}

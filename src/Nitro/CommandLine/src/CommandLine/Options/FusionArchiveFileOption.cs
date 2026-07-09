namespace ChilliCream.Nitro.CommandLine;

internal class FusionArchiveFileOption : Option<string>
{
    public const string OptionName = "--archive";

    public FusionArchiveFileOption() : base(OptionName)
    {
        Description = "The path to a Fusion archive file (the '--configuration' alias is deprecated)";
        Required = true;
        Aliases.Add("-a");
        // This is only here to not break existing scripts
        Aliases.Add("--configuration");
        this.DefaultFromEnvironmentValue(EnvironmentVariables.FusionConfigFile);
        this.LegalFilePathsOnly();
    }
}

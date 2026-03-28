namespace ChilliCream.Nitro.CommandLine.Options;

internal class FusionArchiveFileOption : Option<string>
{
    public const string OptionName = "--archive";

    public FusionArchiveFileOption() : base(OptionName)
    {
        Description = "The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version)";
        Required = true;
        Aliases.Add("-a");
        // This is only here to not break existing scripts
        Aliases.Add("--configuration");
        this.DefaultFromEnvironmentValue("FUSION_CONFIG_FILE");
        this.LegalFilePathsOnly();
    }
}

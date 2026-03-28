namespace ChilliCream.Nitro.CommandLine.Options;

internal class FusionArchiveFileOption : Option<string>
{
    public const string OptionName = "--archive";

    public FusionArchiveFileOption() : base(OptionName)
    {
        Description = "The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version)";
        IsRequired = true;
        AddAlias("-a");
        // This is only here to not break existing scripts
        AddAlias("--configuration");
        this.DefaultFromEnvironmentValue("FUSION_CONFIG_FILE");
        this.LegalFilePathsOnly();
    }
}

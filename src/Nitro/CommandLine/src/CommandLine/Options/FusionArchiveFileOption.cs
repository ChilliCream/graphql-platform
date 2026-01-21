namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FusionArchiveFileOption : Option<FileInfo>
{
    public FusionArchiveFileOption() : this(true)
    {
    }

    public FusionArchiveFileOption(bool isRequired) : base("--archive")
    {
        Description = "The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version)";
        IsRequired = isRequired;
        AddAlias("-a");
        // This is only here to not break existing scripts
        AddAlias("--configuration");
        this.DefaultFileFromEnvironmentValue("FUSION_CONFIG_FILE");
        this.LegalFilePathsOnly();
    }
}

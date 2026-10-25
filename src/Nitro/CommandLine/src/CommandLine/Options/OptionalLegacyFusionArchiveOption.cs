namespace ChilliCream.Nitro.CommandLine;

internal class OptionalLegacyFusionArchiveFileOption : Option<string>
{
    public const string OptionName = "--legacy-v1-archive";

    public OptionalLegacyFusionArchiveFileOption() : base(OptionName)
    {
        Description = "The path to a Fusion v1 archive file. "
            + "This option is only intended to be used during the migration from Fusion v1 to Fusion v2+.";
        Required = false;
        this.LegalFilePathsOnly();
    }
}

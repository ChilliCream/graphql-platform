namespace ChilliCream.Nitro.CommandLine;

internal sealed class FusionPolicyPackOutputOption : Option<string>
{
    public const string OptionName = "--out";

    public FusionPolicyPackOutputOption() : base(OptionName)
    {
        Description = "The output path: a '.far' file, or a directory to write the unpacked bundle into";
        Required = true;
        this.LegalFilePathsOnly();
    }
}

namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionRunArchiveArgument : Argument<string>
{
    public const string ArgumentName = "ARCHIVE_FILE";

    public FusionRunArchiveArgument() : base(ArgumentName)
    {
        Description = "The path to the Fusion archive file";
        this.LegalFilePathsOnly();
    }
}

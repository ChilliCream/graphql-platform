namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionRunArchiveArgument : Argument<string>
{
    public FusionRunArchiveArgument() : base("ARCHIVE_FILE")
    {
        Description = "The path to the Fusion archive file";
        this.LegalFilePathsOnly();
    }
}

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class SourceMetadataOption : Option<string?>
{
    public SourceMetadataOption() : base("--source-metadata")
    {
        Description = "JSON metadata about the environment this invocation is triggered from.";
        IsRequired = false;
    }
}

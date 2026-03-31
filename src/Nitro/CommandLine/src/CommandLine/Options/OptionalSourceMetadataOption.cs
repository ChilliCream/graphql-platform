namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalSourceMetadataOption : Option<string?>
{
    public OptionalSourceMetadataOption() : base("--source-metadata")
    {
        Description = "JSON metadata about the environment";
        Required = false;
        Hidden = true;
    }
}

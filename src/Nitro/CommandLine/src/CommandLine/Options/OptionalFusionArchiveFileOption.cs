namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalFusionArchiveFileOption : FusionArchiveFileOption
{
    public OptionalFusionArchiveFileOption() : base()
    {
        Required = false;
    }
}

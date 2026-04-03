namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalFusionArchiveFileOption : FusionArchiveFileOption
{
    public OptionalFusionArchiveFileOption() : base()
    {
        Required = false;
    }
}

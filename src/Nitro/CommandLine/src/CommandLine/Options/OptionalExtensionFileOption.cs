namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalExtensionFileOption : ExtensionFileOption
{
    public OptionalExtensionFileOption() : base()
    {
        Required = false;
    }
}

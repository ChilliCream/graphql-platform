namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalExtensionFileOption : ExtensionFileOption
{
    public OptionalExtensionFileOption() : base()
    {
        Required = false;
    }
}

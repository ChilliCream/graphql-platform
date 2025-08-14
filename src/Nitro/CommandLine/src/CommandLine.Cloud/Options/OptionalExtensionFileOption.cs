namespace ChilliCream.Nitro.CLI.Option;

internal sealed class OptionalExtensionFileOption : ExtensionFileOption
{
    public OptionalExtensionFileOption() : base()
    {
        IsRequired = false;
    }
}

namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalExtensionFileOption : ExtensionFileOption
{
    public OptionalExtensionFileOption() : base()
    {
        IsRequired = false;
    }
}

namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalTransportDevUrlOption : TransportUrlOption
{
    public const string OptionName = "--dev-url";

    public OptionalTransportDevUrlOption() : base(OptionName)
    {
        Description = "The URL a local development environment uses to reach the source schema";
    }
}

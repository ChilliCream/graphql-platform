namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalTransportUrlOption : TransportUrlOption
{
    public const string OptionName = "--url";

    public OptionalTransportUrlOption() : base(OptionName)
    {
        Description = "The URL the router uses to reach the source schema";
    }
}

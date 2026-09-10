namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalTransportClientNameOption : Option<string>
{
    public const string OptionName = "--client-name";

    public OptionalTransportClientNameOption() : base(OptionName)
    {
        Description = "The name of the HTTP client the gateway uses to reach the source schema";
        this.NonEmptyStringsOnly();
    }
}

namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalApolloFederationVersionOption : Option<string>
{
    public const string OptionName = "--apollo-federation-version";
    public const string Version1 = "1.0";
    public const string Version2 = "2.0";

    public OptionalApolloFederationVersionOption() : base(OptionName)
    {
        Description = "The Apollo Federation version the source schema is built with";
        AcceptOnlyFromAmong(Version1, Version2);
    }
}

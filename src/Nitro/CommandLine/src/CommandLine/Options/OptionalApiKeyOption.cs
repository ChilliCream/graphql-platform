namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalApiKeyOption : Option<string>
{
    public const string OptionName = "--api-key";

    public OptionalApiKeyOption() : base(OptionName)
    {
        Description = "The API key that is used for the authentication";
        Required = false;
        this.DefaultFromEnvironmentValue("API_KEY");
    }
}

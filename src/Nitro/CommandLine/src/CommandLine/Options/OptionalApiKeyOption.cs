namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalApiKeyOption : Option<string>
{
    public const string OptionName = "--api-key";

    public OptionalApiKeyOption() : base(OptionName)
    {
        Description = "The API key used for authentication";
        Required = false;
        this.DefaultFromEnvironmentValue("API_KEY");
    }
}

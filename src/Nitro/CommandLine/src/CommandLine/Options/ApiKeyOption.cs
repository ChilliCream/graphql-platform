namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class ApiKeyOption : Option<string>
{
    public const string OptionName = "--api-key";

    public ApiKeyOption() : base(OptionName)
    {
        Description = "The API key that is used for the authentication";
        Required = false;
        this.DefaultFromEnvironmentValue("API_KEY");
    }
}

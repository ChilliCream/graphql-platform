namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalApiKeyOption : Option<string>
{
    public const string OptionName = "--api-key";

    public OptionalApiKeyOption() : base(OptionName)
    {
        Description = "The API key used for authentication";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ApiKey);
    }
}

namespace ChilliCream.Nitro.CLI.Option;

internal sealed class ApiKeyOption : Option<string>
{
    public ApiKeyOption() : base("--api-key")
    {
        Description = "The api key that is used for the authentication";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_KEY");
    }
}

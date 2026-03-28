namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class ApiKeyNameOption : Option<string>
{
    public ApiKeyNameOption() : base("--name")
    {
        Description = "The name of the API key (for later reference)";
        Required = false;
        this.DefaultFromEnvironmentValue("API_KEY_NAME");
    }
}

namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class ApiKeyNameOption : Option<string>
{
    public ApiKeyNameOption() : base("--name")
    {
        Description = "The name of the api key (for later reference)";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_KEY_NAME");
    }
}

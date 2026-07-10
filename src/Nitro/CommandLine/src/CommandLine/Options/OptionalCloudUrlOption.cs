namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalCloudUrlOption : Option<string>
{
    public const string OptionName = "--cloud-url";

    public OptionalCloudUrlOption() : base(OptionName)
    {
        Description = "The URL of the Nitro backend (only needed for self-hosted or dedicated deployments)";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.CloudUrl);
    }
}

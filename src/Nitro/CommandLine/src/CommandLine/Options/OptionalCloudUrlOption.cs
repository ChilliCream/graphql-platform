using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalCloudUrlOption : Option<string>
{
    public OptionalCloudUrlOption() : base("--cloud-url")
    {
        Description = "The URL of the API.";
        Required = false;
        this.DefaultFromEnvironmentValue("CLOUD_URL", defaultValue: Constants.ApiUrl["https://".Length..]);
    }
}

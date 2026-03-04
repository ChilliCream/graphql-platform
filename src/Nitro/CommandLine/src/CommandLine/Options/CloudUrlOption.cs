using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class CloudUrlOption : Option<string>
{
    public CloudUrlOption() : base("--cloud-url")
    {
        Description = "The URL of the API.";
        IsRequired = false;
        IsHidden = false;
        this.DefaultFromEnvironmentValue("CLOUD_URL", defaultValue: Constants.ApiUrl["https://".Length..]);
    }
}

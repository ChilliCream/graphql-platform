using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class IdentityCloudUrlOption : Option<string>
{
    public IdentityCloudUrlOption() : base("--cloud-url")
    {
        Description = "The url of the api.";
        IsRequired = false;
        IsHidden = false;
        this.DefaultFromEnvironmentValue(
            "CLOUD_URL",
            defaultValue: OidcConfiguration.IdentityUrl["https://".Length..]);
    }
}

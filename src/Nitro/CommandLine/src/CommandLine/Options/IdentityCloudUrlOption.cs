using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class IdentityCloudUrlOption : Option<string>
{
    public IdentityCloudUrlOption() : base("--cloud-url")
    {
        Description = "The URL of the Nitro backend (only needed for self-hosted or dedicated deployments)";
        Required = false;
        this.DefaultFromEnvironmentValue(
            "CLOUD_URL",
            defaultValue: OidcConfiguration.IdentityUrl["https://".Length..]);
    }
}

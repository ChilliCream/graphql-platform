namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class IdentityCloudUrlOption : Option<string>
{
    public IdentityCloudUrlOption() : base("--cloud-url")
    {
        Description = "The url of the api.";
        IsRequired = false;
        IsHidden = false;
        this.DefaultFromEnvironmentValue("CLOUD_URL", defaultValue: OidcConfiguration.IdentityUrl["https://".Length..]);
    }
}

namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class CloudUrlOption : Option<string>
{
    public CloudUrlOption() : base("--cloud-url")
    {
        Description = "The url of the api.";
        IsRequired = false;
        IsHidden = false;
        this.DefaultFromEnvironmentValue("CLOUD_URL", defaultValue: "api.chillicream.com");
    }
}

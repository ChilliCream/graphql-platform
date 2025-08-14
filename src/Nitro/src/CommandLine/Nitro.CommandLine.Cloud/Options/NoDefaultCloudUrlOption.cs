namespace ChilliCream.Nitro.CLI.Option;

internal sealed class NoDefaultCloudUrlOption : Option<string?>
{
    public NoDefaultCloudUrlOption() : base("--cloud-url")
    {
        Description = "The url of the api.";
        IsRequired = false;
        IsHidden = false;
        this.DefaultFromEnvironmentValue("CLOUD_URL");
    }
}

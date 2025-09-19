namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal class DownstreamUrlOption : Option<string>
{
    public DownstreamUrlOption() : base("--url")
    {
        Description = "The url of the downstream service";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("DOWNSTREAM_URL");
    }
}

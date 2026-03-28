namespace ChilliCream.Nitro.CommandLine.Options;

internal class DownstreamUrlOption : Option<string>
{
    public DownstreamUrlOption() : base("--url")
    {
        Description = "The URL of the downstream service";
        Required = true;
        this.DefaultFromEnvironmentValue("DOWNSTREAM_URL");
        this.NonEmptyStringsOnly();
    }
}

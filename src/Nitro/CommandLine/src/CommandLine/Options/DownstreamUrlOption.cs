namespace ChilliCream.Nitro.CommandLine;

internal class DownstreamUrlOption : Option<string>
{
    public DownstreamUrlOption() : base("--url")
    {
        Description = "The URL of the downstream service";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.DownstreamUrl);
        this.NonEmptyStringsOnly();
    }
}

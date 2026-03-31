namespace ChilliCream.Nitro.CommandLine.Options;

internal class ExpiresOption : Option<int>
{
    public ExpiresOption() : base("--expires")
    {
        Description = "The expiration time of the personal access token in days";
        Required = false;
        this.DefaultFromEnvironmentValue("EXPIRES", defaultValue: 180);
    }
}

namespace ChilliCream.Nitro.CommandLine;

internal class ExpiresOption : Option<int>
{
    public ExpiresOption() : base("--expires")
    {
        Description = "The expiration time of the personal access token in days";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Expires, defaultValue: 180);
    }
}

namespace ChilliCream.Nitro.CLI.Option;

internal class ExpiresOption : Option<int>
{
    public ExpiresOption() : base("--expires", "The expiration time of the pat in days")
    {
        IsRequired = false;
        this.DefaultFromEnvironmentValue("EXPIRES", defaultValue: 180);
    }
}

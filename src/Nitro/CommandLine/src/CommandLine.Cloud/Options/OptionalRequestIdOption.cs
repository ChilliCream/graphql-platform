namespace ChilliCream.Nitro.CLI.Option;

internal sealed class OptionalRequestIdOption : Option<string>
{
    public OptionalRequestIdOption() : base("--request-id")
    {
        Description = "The id of a request";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("REQUEST_ID");
    }
}

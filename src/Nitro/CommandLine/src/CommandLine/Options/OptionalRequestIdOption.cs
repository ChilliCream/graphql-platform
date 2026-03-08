namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalRequestIdOption : Option<string>
{
    public OptionalRequestIdOption() : base("--request-id")
    {
        Description = "The ID of a request";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("REQUEST_ID");
    }
}

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class RequestIdOption : Option<string>
{
    public RequestIdOption() : base("--request-id")
    {
        Description = "The ID of a request";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("REQUEST_ID");
    }
}

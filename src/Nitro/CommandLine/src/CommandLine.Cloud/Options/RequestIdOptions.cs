namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class RequestIdOption : Option<string>
{
    public RequestIdOption() : base("--request-id")
    {
        Description = "The id of a request";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("REQUEST_ID");
    }
}

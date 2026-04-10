namespace ChilliCream.Nitro.CommandLine;

internal sealed class RequestIdOption : Option<string>
{
    public RequestIdOption() : base("--request-id")
    {
        Description = "The ID of a request";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.RequestId);
        this.NonEmptyStringsOnly();
    }
}

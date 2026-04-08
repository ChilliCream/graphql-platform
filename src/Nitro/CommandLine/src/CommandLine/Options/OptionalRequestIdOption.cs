namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalRequestIdOption : Option<string>
{
    public OptionalRequestIdOption() : base("--request-id")
    {
        Description = "The ID of a request";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.RequestId);
    }
}

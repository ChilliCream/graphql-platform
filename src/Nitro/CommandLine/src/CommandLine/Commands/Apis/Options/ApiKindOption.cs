namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class ApiKindOption : Option<string>
{
    public ApiKindOption() : base("--kind")
    {
        Description = "The kind of the API (gateway is a legacy alias for router)";
        Required = false;
        // TODO [17]: Remove the legacy gateway input alias.
        this.AcceptOnlyFromAmong("collection", "service", "router", "gateway");
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ApiKind);
    }
}

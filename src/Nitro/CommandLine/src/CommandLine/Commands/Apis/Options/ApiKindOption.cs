namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class ApiKindOption : Option<string>
{
    public ApiKindOption() : base("--kind")
    {
        Description = "The kind of the API";
        Required = false;
        this.AcceptOnlyFromAmong("collection", "service", "gateway");
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ApiKind);
    }
}

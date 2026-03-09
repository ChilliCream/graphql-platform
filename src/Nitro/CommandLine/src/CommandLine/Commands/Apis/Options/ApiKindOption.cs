using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class ApiKindOption : Option<string>
{
    public ApiKindOption() : base("--kind")
    {
        Description = "The kind of the API";
        IsRequired = false;
        this.FromAmong("collection", "service", "gateway");
        this.DefaultFromEnvironmentValue("API_KIND");
    }
}

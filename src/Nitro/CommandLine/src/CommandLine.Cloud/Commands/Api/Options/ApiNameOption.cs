using ChilliCream.Nitro.CommandLine.Cloud.Option;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Api.Options;

internal sealed class ApiNameOption : Option<string>
{
    public ApiNameOption() : base("--name")
    {
        Description = "The name of the api";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_NAME");
    }
}

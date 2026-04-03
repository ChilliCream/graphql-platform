using ChilliCream.Nitro.CommandLine;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class ApiPathOption : Option<string>
{
    public ApiPathOption() : base("--path")
    {
        Description = "The path to the API";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ApiPath);
    }
}

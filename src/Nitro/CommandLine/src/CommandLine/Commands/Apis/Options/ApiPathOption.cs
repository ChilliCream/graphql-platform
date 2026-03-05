using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class ApiPathOption : Option<string>
{
    public ApiPathOption() : base("--path")
    {
        Description = "The path to the API";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_PATH");
    }
}

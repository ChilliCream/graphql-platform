using ChilliCream.Nitro.CLI.Option;

namespace ChilliCream.Nitro.CLI.Commands.Api.Options;

internal sealed class ApiPathOption : Option<string>
{
    public ApiPathOption() : base("--path")
    {
        Description = "The path to the api";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_PATH");
    }
}

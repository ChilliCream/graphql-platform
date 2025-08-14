using ChilliCream.Nitro.CLI.Option;

namespace ChilliCream.Nitro.CLI.Commands.Api.Options;

internal sealed class ApiNameOption : Option<string>
{
    public ApiNameOption() : base("--name")
    {
        Description = "The name of the api";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_NAME");
    }
}

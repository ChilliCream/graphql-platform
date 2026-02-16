using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class ApiNameOption : Option<string>
{
    public ApiNameOption() : base("--name")
    {
        Description = "The name of the api";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_NAME");
    }
}

using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients.Options;

internal sealed class ClientNameOption : Option<string>
{
    public ClientNameOption() : base("--name")
    {
        Description = "The name of the api key (for later reference)";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_KEY_NAME");
    }
}

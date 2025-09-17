using ChilliCream.Nitro.CommandLine.Cloud.Option;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Client;

internal sealed class ClientNameOption : Option<string>
{
    public ClientNameOption() : base("--name")
    {
        Description = "The name of the api key (for later reference)";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("API_KEY_NAME");
    }
}

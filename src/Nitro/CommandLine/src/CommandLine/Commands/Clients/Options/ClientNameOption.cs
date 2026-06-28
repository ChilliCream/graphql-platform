namespace ChilliCream.Nitro.CommandLine.Commands.Clients.Options;

internal sealed class ClientNameOption : Option<string>
{
    public ClientNameOption() : base("--name")
    {
        Description = "The name of the client";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ClientName);
    }
}

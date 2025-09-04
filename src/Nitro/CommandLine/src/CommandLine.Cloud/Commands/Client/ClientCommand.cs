using ChilliCream.Nitro.CommandLine.Cloud.Commands.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ClientCommand : Command
{
    public ClientCommand() : base("client")
    {
        Description = "Manage clients";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new PublishClientCommand());
        AddCommand(new UnpublishClientCommand());
        AddCommand(new ValidateClientCommand());
        AddCommand(new UploadClientCommand());
        AddCommand(new CreateClientCommand());
        AddCommand(new DeleteClientCommand());
        AddCommand(new ListClientCommand());
        AddCommand(new ShowClientCommand());
        AddCommand(new DownloadClientCommand());
    }
}

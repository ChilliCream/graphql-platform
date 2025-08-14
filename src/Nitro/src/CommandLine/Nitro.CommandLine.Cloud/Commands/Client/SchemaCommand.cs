using ChilliCream.Nitro.CLI.Commands.Client;

namespace ChilliCream.Nitro.CLI;

internal sealed class ClientCommand : Command
{
    public ClientCommand() : base("client")
    {
        Description = "Upload, publish and validate clients";

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

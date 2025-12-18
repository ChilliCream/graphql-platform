using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UploadClientCommand : Command
{
    public UploadClientCommand() : base("upload")
    {
        Description = "Upload a new client version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<OperationsFileOption>.Instance);
        AddOption(Opt<ClientIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<OperationsFileOption>.Instance,
            Opt<ClientIdOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        FileInfo operationsFile,
        string clientId,
        CancellationToken cancellationToken)
    {
        console.Title($"Upload operations {operationsFile.FullName.EscapeMarkup()}");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Upload operations...", UploadClient);
        }
        else
        {
            await UploadClient(null);
        }

        return ExitCodes.Success;

        async Task UploadClient(StatusContext? ctx)
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{operationsFile.FullName.EscapeMarkup()}[/]");

            var stream = FileHelpers.CreateFileStream(operationsFile);

            var input = new UploadClientInput
            {
                Operations = new Upload(stream, "operations.graphql"),
                ClientId = clientId,
                Tag = tag
            };

            console.Log("Uploading Client..");
            var result = await client.UploadClient.ExecuteAsync(input, cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UploadClient.Errors);

            if (data.UploadClient.ClientVersion?.Id is null)
            {
                throw new ExitException("Upload operations failed!");
            }

            console.Success("Successfully uploaded operations!");
        }
    }
}

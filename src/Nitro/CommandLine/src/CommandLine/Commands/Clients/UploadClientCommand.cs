using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Configuration;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UploadClientCommand : Command
{
    public UploadClientCommand(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem) : base("upload")
    {
        Description = "Upload a new client version";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<OperationsFileOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(
                console,
                client,
                fileSystem,
                parseResult.GetValue(Opt<TagOption>.Instance)!,
                parseResult.GetValue(Opt<OperationsFileOption>.Instance)!,
                parseResult.GetValue(Opt<ClientIdOption>.Instance)!,
                parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance),
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem,
        string tag,
        string operationsFilePath,
        string clientId,
        string? sourceMetadataJson,
        CancellationToken cancellationToken)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var _ = console.StartActivity("Uploading operations..."))
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{operationsFilePath.EscapeMarkup()}[/]");

            await using var stream = fileSystem.OpenReadStream(operationsFilePath);

            console.Log("Uploading client...");

            await client.UploadClientVersionAsync(
                clientId,
                tag,
                stream,
                source,
                cancellationToken);

            console.Success("Successfully uploaded operations!");
        }

        return ExitCodes.Success;
    }
}

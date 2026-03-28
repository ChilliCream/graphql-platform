using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UploadClientCommand : Command
{
    public UploadClientCommand() : base("upload")
    {
        Description = "Upload a new client version";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<OperationsFileOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(async context =>
        {
            var console = context.BindingContext.GetRequiredService<INitroConsole>();
            var client = context.BindingContext.GetRequiredService<IClientsClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;
            var operationsFilePath = context.ParseResult.GetValueForOption(Opt<OperationsFileOption>.Instance)!;
            var clientId = context.ParseResult.GetValueForOption(Opt<ClientIdOption>.Instance)!;
            var sourceMetadataJson = context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);

            context.ExitCode = await ExecuteAsync(
                console,
                client,
                fileSystem,
                tag,
                operationsFilePath,
                clientId,
                sourceMetadataJson,
                context.GetCancellationToken());
        });
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

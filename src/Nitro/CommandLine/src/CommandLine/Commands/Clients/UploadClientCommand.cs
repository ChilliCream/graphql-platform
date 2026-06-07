using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UploadClientCommand : Command
{
    public UploadClientCommand() : base("upload")
    {
        Description = "Upload a new client version.";

        Options.Add(Opt<OptionalClientIdOption>.Instance);
        Options.Add(Opt<OptionalTagOption>.Instance);
        Options.Add(Opt<OperationsFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client upload \
              --client-id "<client-id>" \
              --tag "v1" \
              --operations-file ./operations.json
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IClientsClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        string clientId;
        var clientIdArg = parseResult.GetValue(Opt<OptionalClientIdOption>.Instance);
        if (console.IsInteractive && clientIdArg is null)
        {
            var apiId = await console.GetOrPromptForApiIdAsync(
                "For which API?", parseResult, apisClient, sessionService, cancellationToken);

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title("Select a client from the list below.")
                .RenderAsync(console, cancellationToken) ?? throw NoClientSelected();

            clientId = selectedClient.Id;
        }
        else
        {
            clientId = parseResult.GetRequiredOptionalValue(Opt<OptionalClientIdOption>.Instance);
        }

        var tag = await console.GetOrPromptForTagAsync(
            "Which tag?", parseResult, Opt<OptionalTagOption>.Instance, cancellationToken);

        var operationsFilePath = parseResult.GetRequiredValue(Opt<OperationsFileOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        if (!Path.IsPathRooted(operationsFilePath))
        {
            operationsFilePath = Path.Combine(fileSystem.GetCurrentDirectory(), operationsFilePath);
        }

        if (!fileSystem.FileExists(operationsFilePath))
        {
            throw new ExitException(Messages.OperationsFileDoesNotExist(operationsFilePath));
        }

        await using var activity = console.StartActivity(
            $"Uploading new version '{tag.EscapeMarkup()}' for client '{clientId.EscapeMarkup()}'",
            "Failed to upload a new client version.");

        await using var stream = fileSystem.OpenReadStream(operationsFilePath);

        var data = await client.UploadClientVersionAsync(
            clientId,
            tag,
            stream,
            source,
            cancellationToken);

        if (data.Errors?.Count > 0)
        {
            await activity.FailAllAsync();

            foreach (var error in data.Errors)
            {
                var errorMessage = error switch
                {
                    IUploadClient_UploadClient_Errors_UnauthorizedOperation err => err.Message,
                    IUploadClient_UploadClient_Errors_ClientNotFoundError err => err.Message,
                    IUploadClient_UploadClient_Errors_DuplicatedTagError err => err.Message,
                    IUploadClient_UploadClient_Errors_ConcurrentOperationError err => err.Message,
                    IUploadClient_UploadClient_Errors_InvalidPersistedQueryError err => err.Message,
                    IUploadClient_UploadClient_Errors_InvalidSourceMetadataInputError err => err.Message,
                    IError err => Messages.UnexpectedMutationError(err),
                    _ => Messages.UnexpectedMutationError()
                };

                console.Error.WriteErrorLine(errorMessage);
            }

            return ExitCodes.Error;
        }

        if (data.ClientVersion is null)
        {
            throw Exit("Could not upload client.");
        }

        activity.Success($"Uploaded new client version '{tag.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}

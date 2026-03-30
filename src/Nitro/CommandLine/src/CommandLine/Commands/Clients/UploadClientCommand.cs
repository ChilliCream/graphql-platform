using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UploadClientCommand : Command
{
    public UploadClientCommand(
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("upload")
    {
        Description = "Upload a new client version";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<OperationsFileOption>.Instance);
        Options.Add(Opt<ClientIdOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                fileSystem,
                sessionService,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var operationsFilePath = parseResult.GetValue(Opt<OperationsFileOption>.Instance)!;
        var clientId = parseResult.GetValue(Opt<ClientIdOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity($"Uploading new client version '{tag.EscapeMarkup()}' for client '{clientId.EscapeMarkup()}'"))
        {
            await using var stream = fileSystem.OpenReadStream(operationsFilePath);

            var data = await client.UploadClientVersionAsync(
                clientId,
                tag,
                stream,
                source,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail("Failed to upload a new client version.");

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
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    await console.Error.WriteLineAsync(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.ClientVersion is null)
            {
                throw Exit("Could not upload client.");
            }

            activity.Success($"Uploaded new client version '{tag.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(new UploadClientResult
            {
                ClientVersionId = data.ClientVersion.Id,
                ClientId = clientId,
                Tag = tag
            }));

            return ExitCodes.Success;
        }
    }

    public class UploadClientResult
    {
        public required string ClientVersionId { get; init; }

        public required string ClientId { get; init; }

        public required string Tag { get; init; }
    }
}

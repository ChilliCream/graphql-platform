using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class ValidateMcpFeatureCollectionCommand : Command
{
    public ValidateMcpFeatureCollectionCommand() : base("validate")
    {
        Description = "Validate an MCP feature collection version.";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<McpFeatureCollectionIdOption>.Instance);
        Options.Add(Opt<McpPromptFilePatternOption>.Instance);
        Options.Add(Opt<McpToolFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(async (services, parseResult, cancellationToken) =>
        {
            var console = services.GetRequiredService<INitroConsole>();
            var client = services.GetRequiredService<IMcpClient>();
            var fileSystem = services.GetRequiredService<IFileSystem>();
            return await ExecuteAsync(parseResult, console, client, fileSystem, cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IMcpClient client,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var mcpFeatureCollectionId = parseResult.GetValue(Opt<McpFeatureCollectionIdOption>.Instance)!;
        var promptPatterns = parseResult.GetValue(Opt<McpPromptFilePatternOption>.Instance)!;
        var toolPatterns = parseResult.GetValue(Opt<McpToolFilePatternOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Validating MCP feature collection against stage '{stage.EscapeMarkup()}'",
            "Failed to validate the MCP feature collection."))
        {
            var promptFiles = fileSystem.GlobMatch(promptPatterns, ["**/bin/**", "**/obj/**"]).ToArray();
            var toolFiles = fileSystem.GlobMatch(toolPatterns, ["**/bin/**", "**/obj/**"]).ToArray();

            activity.Update($"Found {promptFiles.Length} prompt(s) and {toolFiles.Length} tool(s).");

            if (promptFiles.Length < 1 && toolFiles.Length < 1)
            {
                activity.Fail();
                console.Error.WriteErrorLine(
                    "Could not find any MCP prompt or tool definition files with the provided patterns.");
                return ExitCodes.Error;
            }

            var archiveStream =
                await McpFeatureCollectionHelpers.BuildMcpFeatureCollectionArchive(
                    fileSystem,
                    promptFiles,
                    toolFiles,
                    ct);

            var validationRequest = await client.StartMcpFeatureCollectionValidationAsync(
                mcpFeatureCollectionId,
                stage,
                archiveStream,
                source,
                ct);

            if (validationRequest.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in validationRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_UnauthorizedOperation err => err.Message,
                        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_StageNotFoundError err => err.Message,
                        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create validation request!");
            }

            activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()})");

            await foreach (var update in client.SubscribeToMcpFeatureCollectionValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case IMcpFeatureCollectionVersionValidationFailed { Errors: var errors }:
                        activity.Fail();

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IUnexpectedProcessingError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case IMcpFeatureCollectionValidationError e:
                                    console.PrintMcpFeatureCollectionValidationErrors(e);
                                    break;
                                case IMcpFeatureCollectionValidationArchiveError e:
                                    console.Error.WriteErrorLine(ErrorMessages.InvalidArchive(e.Message));
                                    break;
                                case IError e:
                                    console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        console.Error.WriteErrorLine("MCP Feature Collection validation failed.");
                        return ExitCodes.Error;

                    case IMcpFeatureCollectionVersionValidationSuccess:
                        activity.Success($"Validated MCP feature collection against stage '{stage.EscapeMarkup()}'.");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("Validating...");
                        break;

                    default:
                        activity.Warning("Unknown server response. Consider updating the CLI.");
                        break;
                }
            }

            activity.Fail();
        }

        return ExitCodes.Error;
    }
}

using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class ValidateMcpFeatureCollectionCommand : Command
{
    public ValidateMcpFeatureCollectionCommand(
        INitroConsole console,
        IMcpClient client,
        IFileSystem fileSystem) : base("validate")
    {
        Description = "Validate an MCP Feature Collection version";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<McpFeatureCollectionIdOption>.Instance);
        Options.Add(Opt<McpPromptFilePatternOption>.Instance);
        Options.Add(Opt<McpToolFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, fileSystem, cancellationToken));
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

        await using (var activity = console.StartActivity($"Validating MCP feature collection against stage '{stage.EscapeMarkup()}'"))
        {
            // console.Log("Searching for MCP prompt definition files with the following patterns:");
            // foreach (var promptPattern in promptPatterns)
            // {
            //     console.Log($"- {promptPattern}");
            // }
            //
            // console.Log("Searching for MCP tool definition files with the following patterns:");
            // foreach (var toolPattern in toolPatterns)
            // {
            //     console.Log($"- {toolPattern}");
            // }

            var promptFiles = fileSystem.GlobMatch(promptPatterns, ["**/bin/**", "**/obj/**"]).ToArray();
            var toolFiles = fileSystem.GlobMatch(toolPatterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (promptFiles.Length < 1 && toolFiles.Length < 1)
            {
                activity.Fail("Failed to validate the MCP feature collection.");
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
                activity.Fail("Failed to validate the MCP feature collection.");

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
                        activity.Fail("Failed to validate the MCP feature collection.");

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
                                    console.Error.WriteErrorLine(e.Message);
                                    break;
                                case IError e:
                                    console.Error.WriteErrorLine("Unexpected error: " + e.Message);
                                    break;
                            }
                        }

                        console.Error.WriteErrorLine("MCP Feature Collection validation failed.");
                        return ExitCodes.Error;

                    case IMcpFeatureCollectionVersionValidationSuccess:
                        activity.Success("Validated the MCP feature collection.");
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

            activity.Fail("Failed to validate the MCP feature collection.");
        }

        return ExitCodes.Error;
    }
}

using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class ValidateMcpFeatureCollectionCommand : Command
{
    public ValidateMcpFeatureCollectionCommand() : base("validate")
    {
        Description = "Validate an MCP feature collection version.";

        Options.Add(Opt<McpFeatureCollectionIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<McpPromptFilePatternOption>.Instance);
        Options.Add(Opt<McpToolFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            mcp validate \
              --mcp-feature-collection-id "<collection-id>" \
              --stage "dev" \
              --prompt-pattern "./prompts/**/*.json" \
              --tool-pattern "./tools/**/*.graphql"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IMcpClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var mcpFeatureCollectionId = parseResult.GetRequiredValue(Opt<McpFeatureCollectionIdOption>.Instance);
        var promptPatterns = parseResult.GetRequiredValue(Opt<McpPromptFilePatternOption>.Instance);
        var toolPatterns = parseResult.GetRequiredValue(Opt<McpToolFilePatternOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Validating MCP feature collection '{mcpFeatureCollectionId.EscapeMarkup()}' against stage '{stage.EscapeMarkup()}'",
            "Failed to validate the MCP feature collection."))
        {
            var promptFiles = fileSystem.GlobMatch(promptPatterns, ["**/bin/**", "**/obj/**"]).ToArray();
            var toolFiles = fileSystem.GlobMatch(toolPatterns, ["**/bin/**", "**/obj/**"]).ToArray();

            activity.Update($"Found {promptFiles.Length} prompt(s) and {toolFiles.Length} tool(s).");

            if (promptFiles.Length < 1 && toolFiles.Length < 1)
            {
                await activity.FailAllAsync();
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
                await activity.FailAllAsync();

                foreach (var error in validationRequest.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IInvalidSourceMetadataInputError err => err.Message,
                        IStageNotFoundError err => err.Message,
                        IMcpFeatureCollectionNotFoundError err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (validationRequest.Id is not { } id)
            {
                throw new ExitException("Could not create validation request!");
            }

            activity.Update($"Validation request created. {$"(ID: {id})".Dim()}");

            await foreach (var update in client.SubscribeToMcpFeatureCollectionValidationAsync(id, ct))
            {
                switch (update)
                {
                    case IMcpFeatureCollectionVersionValidationFailed { Errors: var errors }:
                        var errorTree = new Tree("");

                        foreach (var error in errors)
                        {
                            switch (error)
                            {
                                case IMcpFeatureCollectionValidationError e:
                                    errorTree.AddMcpFeatureCollectionValidationErrors(e);
                                    break;
                                case IMcpFeatureCollectionValidationArchiveError e:
                                    errorTree.AddErrorMessage(Messages.InvalidArchive(e.Message));
                                    break;
                                case IUnexpectedProcessingError e:
                                    errorTree.AddErrorMessage(e.Message);
                                    break;
                                case IProcessingTimeoutError e:
                                    errorTree.AddErrorMessage(e.Message);
                                    break;
                            }
                        }

                        await activity.FailAllAsync(errorTree, "MCP feature collection failed validation.");

                        throw new ExitException("MCP feature collection failed validation.");

                    case IMcpFeatureCollectionVersionValidationSuccess:
                        activity.Success("MCP feature collection passed validation.");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update(Messages.Validating);
                        break;

                    default:
                        activity.Update(Messages.UnknownServerResponse, ActivityUpdateKind.Warning);
                        break;
                }
            }

            await activity.FailAllAsync();
        }

        return ExitCodes.Error;
    }
}

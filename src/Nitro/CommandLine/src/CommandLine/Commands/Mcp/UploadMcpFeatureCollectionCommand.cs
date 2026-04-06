using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class UploadMcpFeatureCollectionCommand : Command
{
    public UploadMcpFeatureCollectionCommand() : base("upload")
    {
        Description = "Upload a new MCP feature collection version.";

        Options.Add(Opt<McpFeatureCollectionIdOption>.Instance);
        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<McpPromptFilePatternOption>.Instance);
        Options.Add(Opt<McpToolFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            mcp upload \
              --mcp-feature-collection-id "<collection-id>" \
              --tag "v1" \
              --prompt-pattern "./prompts/**/*.json" \
              --tool-pattern "./tools/**/*.graphql"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IMcpClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var promptPatterns = parseResult.GetRequiredValue(Opt<McpPromptFilePatternOption>.Instance);
        var toolPatterns = parseResult.GetRequiredValue(Opt<McpToolFilePatternOption>.Instance);
        var mcpFeatureCollectionId = parseResult.GetRequiredValue(Opt<McpFeatureCollectionIdOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        var promptFiles = fileSystem.GlobMatch(promptPatterns, ["**/bin/**", "**/obj/**"]).ToArray();
        var toolFiles = fileSystem.GlobMatch(toolPatterns, ["**/bin/**", "**/obj/**"]).ToArray();

        if (promptFiles.Length < 1 && toolFiles.Length < 1)
        {
            throw new ExitException(
                "Could not find any MCP prompt or tool definition files with the provided patterns.");
        }

        var archiveStream =
            await McpFeatureCollectionHelpers.BuildMcpFeatureCollectionArchive(
                fileSystem,
                promptFiles,
                toolFiles,
                cancellationToken);

        await using (var activity = console.StartActivity(
            $"Uploading new MCP feature collection version '{tag.EscapeMarkup()}' for collection '{mcpFeatureCollectionId.EscapeMarkup()}'",
            "Failed to upload a new MCP feature collection version."))
        {
            activity.Update($"Found {promptFiles.Length} prompt(s) and {toolFiles.Length} tool(s).");

            var data = await client.UploadMcpFeatureCollectionVersionAsync(
                mcpFeatureCollectionId,
                tag,
                archiveStream,
                source,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IMcpFeatureCollectionNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IInvalidSourceMetadataInputError err => err.Message,
                        IDuplicatedTagError err => err.Message,
                        IConcurrentOperationError err => err.Message,
                        IInvalidMcpFeatureCollectionArchiveError err =>
                            ErrorMessages.InvalidArchive(err.Message),
                        IError err => ErrorMessages.UnexpectedMutationError(err),
                        _ => ErrorMessages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.McpFeatureCollectionVersion is null)
            {
                activity.Fail();
                console.Error.WriteErrorLine("Could not upload MCP Feature Collection version.");
                return ExitCodes.Error;
            }

            activity.Success($"Uploaded new MCP feature collection version '{tag.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }
    }
}

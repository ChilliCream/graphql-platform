using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class UploadMcpFeatureCollectionCommand : Command
{
    public UploadMcpFeatureCollectionCommand(
        INitroConsole console,
        IMcpClient client,
        IFileSystem fileSystem,
        ISessionService sessionService) : base("upload")
    {
        Description = "Upload a new MCP Feature Collection version";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<McpFeatureCollectionIdOption>.Instance);
        Options.Add(Opt<McpPromptFilePatternOption>.Instance);
        Options.Add(Opt<McpToolFilePatternOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, fileSystem, sessionService, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IMcpClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var promptPatterns = parseResult.GetValue(Opt<McpPromptFilePatternOption>.Instance)!;
        var toolPatterns = parseResult.GetValue(Opt<McpToolFilePatternOption>.Instance)!;
        var mcpFeatureCollectionId = parseResult.GetValue(Opt<McpFeatureCollectionIdOption>.Instance)!;
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

        await using (var activity = console.StartActivity($"Uploading new MCP feature collection version '{tag.EscapeMarkup()}' for collection '{mcpFeatureCollectionId.EscapeMarkup()}'"))
        {
            var data = await client.UploadMcpFeatureCollectionVersionAsync(
                mcpFeatureCollectionId,
                tag,
                archiveStream,
                source,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail("Failed to upload a new MCP feature collection version.");

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IMcpFeatureCollectionNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IDuplicatedTagError err => err.Message,
                        IConcurrentOperationError err => err.Message,
                        IInvalidMcpFeatureCollectionArchiveError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    await console.Error.WriteLineAsync(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.McpFeatureCollectionVersion is null)
            {
                activity.Fail("Failed to upload a new MCP feature collection version.");
                await console.Error.WriteLineAsync("Could not upload MCP Feature Collection version.");
                return ExitCodes.Error;
            }

            activity.Success($"Uploaded new MCP feature collection version '{tag.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }
    }
}

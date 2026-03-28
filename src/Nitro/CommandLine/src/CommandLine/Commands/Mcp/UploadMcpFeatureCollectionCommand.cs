using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class UploadMcpFeatureCollectionCommand : Command
{
    public UploadMcpFeatureCollectionCommand(
        INitroConsole console,
        IMcpClient client,
        IFileSystem fileSystem) : base("upload")
    {
        Description = "Upload a new MCP Feature Collection version";

        Options.Add(Opt<TagOption>.Instance);
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
        CancellationToken cancellationToken)
    {
        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var promptPatterns = parseResult.GetValue(Opt<McpPromptFilePatternOption>.Instance)!;
        var toolPatterns = parseResult.GetValue(Opt<McpToolFilePatternOption>.Instance)!;
        var mcpFeatureCollectionId = parseResult.GetValue(Opt<McpFeatureCollectionIdOption>.Instance)!;
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var _ = console.StartActivity("Uploading new MCP Feature Collection version..."))
        {
            await UploadMcpFeatureCollection();
        }

        return ExitCodes.Success;

        async Task UploadMcpFeatureCollection()
        {
            console.Log("Searching for MCP prompt definition files with the following patterns:");
            foreach (var promptPattern in promptPatterns)
            {
                console.Log($"- {promptPattern}");
            }

            console.Log("Searching for MCP tool definition files with the following patterns:");
            foreach (var toolPattern in toolPatterns)
            {
                console.Log($"- {toolPattern}");
            }

            var promptFiles = fileSystem.GlobMatch(promptPatterns, ["**/bin/**", "**/obj/**"]).ToArray();
            var toolFiles = fileSystem.GlobMatch(toolPatterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (promptFiles.Length < 1 && toolFiles.Length < 1)
            {
                console.WriteLine("Could not find any MCP prompt or tool definition files with the provided patterns.");
                return;
            }

            console.Log($"Found {promptFiles.Length} MCP prompt definition file(s).");
            console.Log($"Found {toolFiles.Length} MCP tool definition file(s).");

            var archiveStream =
                await McpFeatureCollectionHelpers.BuildMcpFeatureCollectionArchive(
                    fileSystem,
                    promptFiles,
                    toolFiles,
                    cancellationToken);

            console.Log("Uploading MCP Feature Collection..");
            await client.UploadMcpFeatureCollectionVersionAsync(
                mcpFeatureCollectionId,
                tag,
                archiveStream,
                source,
                cancellationToken);

            console.Success("Successfully uploaded new MCP Feature Collection version!");
        }
    }
}

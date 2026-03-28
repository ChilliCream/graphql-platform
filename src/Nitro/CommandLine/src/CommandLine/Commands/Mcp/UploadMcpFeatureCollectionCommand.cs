using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class UploadMcpFeatureCollectionCommand : Command
{
    public UploadMcpFeatureCollectionCommand() : base("upload")
    {
        Description = "Upload a new MCP Feature Collection version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<McpFeatureCollectionIdOption>.Instance);
        AddOption(Opt<McpPromptFilePatternOption>.Instance);
        AddOption(Opt<McpToolFilePatternOption>.Instance);
        AddOption(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(async context =>
        {
            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var client = context.BindingContext.GetRequiredService<IMcpClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;
            var promptPatterns = context.ParseResult.GetValueForOption(Opt<McpPromptFilePatternOption>.Instance)!;
            var toolPatterns = context.ParseResult.GetValueForOption(Opt<McpToolFilePatternOption>.Instance)!;
            var mcpFeatureCollectionId = context.ParseResult.GetValueForOption(Opt<McpFeatureCollectionIdOption>.Instance)!;
            var sourceMetadataJson = context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);

            context.ExitCode = await ExecuteAsync(
                console,
                client,
                fileSystem,
                tag,
                promptPatterns,
                toolPatterns,
                mcpFeatureCollectionId,
                sourceMetadataJson,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IMcpClient client,
        IFileSystem fileSystem,
        string tag,
        List<string> promptPatterns,
        List<string> toolPatterns,
        string mcpFeatureCollectionId,
        string? sourceMetadataJson,
        CancellationToken cancellationToken)
    {
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

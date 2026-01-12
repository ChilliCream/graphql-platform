using System.Text;
using System.Text.Json;
using HotChocolate.Adapters.Mcp.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal static class McpFeatureCollectionHelpers
{
    public static async Task<MemoryStream> BuildMcpFeatureCollectionArchive(
        IEnumerable<string> promptFiles,
        IEnumerable<string> toolFiles,
        CancellationToken cancellationToken)
    {
        var archiveStream = new MemoryStream();
        var collectionArchive = McpFeatureCollectionArchive.Create(archiveStream, leaveOpen: true);

        await collectionArchive.SetArchiveMetadataAsync(
            new ArchiveMetadata(),
            cancellationToken);

        foreach (var promptFile in promptFiles)
        {
            if (!Path.GetExtension(promptFile).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var promptName = Path.GetFileNameWithoutExtension(promptFile);
            var settings =
                await JsonDocument.ParseAsync(File.OpenRead(promptFile), cancellationToken: cancellationToken);

            await collectionArchive.AddPromptAsync(promptName, settings, cancellationToken);
        }

        foreach (var toolFile in toolFiles)
        {
            if (!Path.GetExtension(toolFile).Equals(".graphql", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var toolName = Path.GetFileNameWithoutExtension(toolFile);
            var documentBytes = Encoding.UTF8.GetBytes(await File.ReadAllTextAsync(toolFile, cancellationToken));
            var settingsFile = Path.ChangeExtension(toolFile, ".json");
            var openAiComponentFile = Path.ChangeExtension(toolFile, ".open-ai.html");

            JsonDocument? settings = null;
            if (File.Exists(settingsFile))
            {
                settings =
                    await JsonDocument.ParseAsync(File.OpenRead(toolFile), cancellationToken: cancellationToken);
            }

            byte[]? openAiComponent = null;
            if (File.Exists(openAiComponentFile))
            {
                openAiComponent = await File.ReadAllBytesAsync(openAiComponentFile, cancellationToken);
            }

            await collectionArchive.AddToolAsync(
                toolName,
                documentBytes,
                settings,
                openAiComponent,
                cancellationToken);
        }

        await collectionArchive.CommitAsync(cancellationToken);
        collectionArchive.Dispose();

        return archiveStream;
    }
}

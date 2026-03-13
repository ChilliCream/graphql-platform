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
            await using var settingsStream = File.OpenRead(promptFile);
            var settings = await JsonDocument.ParseAsync(settingsStream, cancellationToken: cancellationToken);

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
            var viewFile = Path.ChangeExtension(toolFile, ".html");

            JsonDocument? settings = null;
            if (File.Exists(settingsFile))
            {
                await using var settingsStream = File.OpenRead(settingsFile);
                settings = await JsonDocument.ParseAsync(settingsStream, cancellationToken: cancellationToken);
            }

            byte[]? view = null;
            if (File.Exists(viewFile))
            {
                view = await File.ReadAllBytesAsync(viewFile, cancellationToken);
            }

            await collectionArchive.AddToolAsync(
                toolName,
                documentBytes,
                settings,
                view,
                cancellationToken);
        }

        await collectionArchive.CommitAsync(cancellationToken);
        collectionArchive.Dispose();

        archiveStream.Position = 0;

        return archiveStream;
    }
}

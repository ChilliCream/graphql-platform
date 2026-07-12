using System.Text.Json;
using HotChocolate.Fusion.SourceSchema.Packaging;

namespace ChilliCream.Nitro.CommandLine;

internal static class FusionSourceSchemaArchiveHelper
{
    public static async Task<Stream> CreateArchiveStreamAsync(
        ReadOnlyMemory<byte> schema,
        JsonDocument settings,
        ReadOnlyMemory<byte> schemaExtensions = default,
        CancellationToken cancellationToken = default)
    {
        var archiveStream = new MemoryStream();
        var archive = FusionSourceSchemaArchive.Create(archiveStream, leaveOpen: true);

        await archive.SetArchiveMetadataAsync(new ArchiveMetadata(), cancellationToken);
        await archive.SetSchemaAsync(schema, cancellationToken);

        if (schemaExtensions.Length > 0)
        {
            await archive.SetSchemaExtensionsAsync(schemaExtensions, cancellationToken);
        }

        await archive.SetSettingsAsync(settings, cancellationToken);

        await archive.CommitAsync(cancellationToken);
        archive.Dispose();

        archiveStream.Position = 0;

        return archiveStream;
    }
}

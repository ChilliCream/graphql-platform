using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Packaging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Builds fusion archives that stand in for the fusion configurations that Nitro serves.
/// </summary>
internal static class NitroTestArchive
{
    public static async Task<byte[]> CreateAsync(
        CancellationToken cancellationToken,
        params string[] sourceSchemaNames)
    {
        await using var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(
                new ArchiveMetadata
                {
                    SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                    SourceSchemas = [..sourceSchemaNames]
                },
                cancellationToken);

            await archive.CommitAsync(cancellationToken);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Creates an archive that carries the schema and the settings of every source schema, which
    /// is what a fusion configuration of Nitro looks like.
    /// </summary>
    public static async Task<byte[]> CreateAsync(
        CancellationToken cancellationToken,
        params NitroTestSourceSchema[] sourceSchemas)
    {
        await using var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(
                new ArchiveMetadata
                {
                    SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                    SourceSchemas = [..sourceSchemas.Select(sourceSchema => sourceSchema.Name)]
                },
                cancellationToken);

            foreach (var sourceSchema in sourceSchemas)
            {
                using var settings = JsonDocument.Parse(sourceSchema.Settings);

                await archive.SetSourceSchemaConfigurationAsync(
                    sourceSchema.Name,
                    Encoding.UTF8.GetBytes(sourceSchema.Schema),
                    settings,
                    cancellationToken: cancellationToken);
            }

            await archive.CommitAsync(cancellationToken);
        }

        return stream.ToArray();
    }

    public static async Task<ImmutableArray<string>> ReadSourceSchemaNamesAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        using var archive = FusionArchive.Open(archivePath);
        var names = await archive.GetSourceSchemaNamesAsync(cancellationToken);

        return [..names];
    }
}

/// <summary>
/// A source schema of a fusion configuration that Nitro serves.
/// </summary>
/// <param name="Name">
/// The name of the source schema.
/// </param>
/// <param name="Schema">
/// The source schema document.
/// </param>
/// <param name="Settings">
/// The source schema settings document.
/// </param>
internal sealed record NitroTestSourceSchema(string Name, string Schema, string Settings);

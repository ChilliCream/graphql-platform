using System.Text.Json;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal static class LegacyFusionArchiveMigrator
{
    public static async Task<CompositionSettings?> MergeIntoAsync(
        MemoryStream legacyBuffer,
        Dictionary<string, (SourceSchemaText, JsonDocument)> sourceSchemas,
        IReadOnlyCollection<string> explicitNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(legacyBuffer);
        ArgumentNullException.ThrowIfNull(sourceSchemas);
        ArgumentNullException.ThrowIfNull(explicitNames);

        legacyBuffer.Position = 0;

        await using var package = FusionGraphPackage.Open(legacyBuffer, FileAccess.Read);

        var explicitSet = new HashSet<string>(explicitNames, StringComparer.Ordinal);

        var configurations = await package.GetSubgraphConfigurationsAsync(cancellationToken);

        foreach (var configuration in configurations)
        {
            if (explicitSet.Contains(configuration.Name))
            {
                continue;
            }

            if (sourceSchemas.ContainsKey(configuration.Name))
            {
                continue;
            }

            if (configuration.Extensions.Count > 0)
            {
                throw new ExitException(
                    Messages.LegacyArchiveSchemaExtensionsNotSupported(configuration.Name));
            }

            var rawBytes = await package.TryGetSubgraphConfigurationRawAsync(
                configuration.Name,
                cancellationToken);

            if (rawBytes is null)
            {
                throw new ExitException(
                    Messages.LegacyArchiveCorrupt(
                        "<buffer>",
                        $"subgraph '{configuration.Name}' is missing its subgraph-config part."));
            }

            var migratedDoc = FusionMigrationHelpers.MigrateSubgraphConfig(rawBytes.Value);

            sourceSchemas[configuration.Name] =
                (new SourceSchemaText(configuration.Name, configuration.Schema), migratedDoc);
        }

        CompositionSettings? migratedSettings = null;

        var rawSettings = await package.TryGetFusionGraphSettingsRawAsync(cancellationToken);

        if (rawSettings is not null)
        {
            using var settingsDoc = FusionMigrationHelpers.MigrateGatewaySettings(rawSettings.Value);
            migratedSettings = settingsDoc.Deserialize(
                SettingsJsonSerializerContext.Default.CompositionSettings);
        }

        legacyBuffer.Position = 0;

        return migratedSettings;
    }
}

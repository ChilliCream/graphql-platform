using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class AspireCompositionHelper
{
    public static Task<bool> TryComposeArchivesAsync(
        string fusionArchivePath,
        IReadOnlyList<SourceSchemaArchiveInfo> archives,
        GraphQLCompositionSettings settings,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
        => TryComposeArchivesAsync(
            fusionArchivePath,
            archives,
            settings.EnvironmentName ?? "Aspire",
            settings,
            logger,
            cancellationToken);

    public static async Task<bool> TryComposeArchivesAsync(
        string fusionArchivePath,
        IReadOnlyList<SourceSchemaArchiveInfo> archives,
        string environmentName,
        GraphQLCompositionSettings settings,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        var sourceSchemas = new List<SourceSchemaInfo>(archives.Count);

        try
        {
            foreach (var archiveInfo in archives)
            {
                using var archive = FusionSourceSchemaArchive.Open(
                    archiveInfo.ArchivePath);
                var schema = await archive.TryGetSchemaAsync(cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Fusion source archive '{archiveInfo.Name}' has no schema.");
                var sourceSettings =
                    await archive.TryGetSettingsAsync(cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Fusion source archive '{archiveInfo.Name}' has no settings.");
                var extensions =
                    await archive.TryGetSchemaExtensionsAsync(cancellationToken);

                sourceSchemas.Add(
                    new SourceSchemaInfo
                    {
                        Name = archiveInfo.Name,
                        Schema = new SourceSchemaText(
                            archiveInfo.Name,
                            Encoding.UTF8.GetString(schema.Span),
                            extensions is null
                                ? null
                                : Encoding.UTF8.GetString(extensions.Value.Span)),
                        SchemaSettings = sourceSettings
                    });
            }

            return await TryComposeAsync(
                fusionArchivePath,
                [.. sourceSchemas],
                environmentName,
                settings,
                logger,
                cancellationToken);
        }
        finally
        {
            foreach (var sourceSchema in sourceSchemas)
            {
                sourceSchema.SchemaSettings.Dispose();
            }
        }
    }

    public static Task<bool> TryComposeAsync(
        string fusionArchivePath,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        GraphQLCompositionSettings settings,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
        => TryComposeAsync(
            fusionArchivePath,
            newSourceSchemas,
            settings.EnvironmentName ?? "Aspire",
            settings,
            logger,
            cancellationToken);

    public static async Task<bool> TryComposeAsync(
        string fusionArchivePath,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        string environmentName,
        GraphQLCompositionSettings settings,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        using var archive = File.Exists(fusionArchivePath)
            ? FusionArchive.Open(fusionArchivePath, FusionArchiveMode.Update)
            : FusionArchive.Create(fusionArchivePath);

        var compositionLog = new CompositionLog();
        var compositionSettings = CreateCompositionSettings(settings);
        var sourceSchemas = newSourceSchemas.ToDictionary(
            s => s.Name,
            s => (s.Schema, s.SchemaSettings));

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            sourceSchemas,
            archive,
            environmentName,
            compositionSettings,
            legacyArchive: null,
            cancellationToken);

        var output = new StringBuilder();

        foreach (var entry in compositionLog)
        {
            if (entry.Severity is LogSeverity.Error)
            {
                output.AppendLine($"‼️ {FormatMultilineMessage(entry.Message)}");
            }
            else
            {
                output.AppendLine(entry.Message);
            }
        }

        if (result.IsFailure)
        {
            output.Append("❌ Composition failed:");
            logger.LogError("{Message}", output.ToString());
            return false;
        }

        output.Append("✅ Composition completed successfully.");
        logger.LogInformation("{Message}", output.ToString());

        return true;
    }

    internal static JsonDocument ResolveSourceSchemaSettings(
        JsonDocument sourceSchemaSettings,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(sourceSchemaSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        using var buffer = new PooledArrayWriter();
        new SettingsComposer().Compose(
            buffer,
            [sourceSchemaSettings.RootElement],
            environmentName);
        using var gatewaySettings = JsonDocument.Parse(buffer.WrittenMemory);
        var sourceSchemas = gatewaySettings.RootElement
            .GetProperty("sourceSchemas");
        var resolvedSettings = sourceSchemas
            .EnumerateObject()
            .Single()
            .Value;

        return JsonSerializer.SerializeToDocument(resolvedSettings);
    }

    internal static CompositionSettings CreateCompositionSettings(
        GraphQLCompositionSettings settings)
    {
        return new CompositionSettings
        {
            Merger =
            {
                CacheControlMergeBehavior = settings.CacheControlMergeBehavior,
                EnableGlobalObjectIdentification = settings.EnableGlobalObjectIdentification,
                NodeResolution = settings.NodeResolution,
                TagMergeBehavior = settings.TagMergeBehavior
            },
            Satisfiability = { IncludeSatisfiabilityPaths = settings.IncludeSatisfiabilityPaths },
            ApolloFederationCompatibility =
            {
                AllowNonResolvableInterfaceObjects = settings.AllowNonResolvableInterfaceObjects,
                ShareableFieldRuntimeTypeRouting = settings.ShareableFieldRuntimeTypeRouting
            },
            Preprocessor = { ExcludeByTag = settings.ExcludeByTag?.ToHashSet() }
        };
    }

    /// <summary>
    /// Since we're prefixing the message with an emoji and space before printing,
    /// we need to also indent each line of a multiline message by three spaces to fix the alignment.
    /// </summary>
    private static string FormatMultilineMessage(string message)
    {
        var lines = message.Split(Environment.NewLine);

        if (lines.Length <= 1)
        {
            return message;
        }

        return string.Join(Environment.NewLine + "   ", lines);
    }
}

internal readonly record struct SourceSchemaArchiveInfo(
    string Name,
    string ArchivePath);

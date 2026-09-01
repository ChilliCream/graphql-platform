using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// Composes the given source schema archives into a fusion archive, resolving the source
    /// schema settings against the environment of the distributed application.
    /// </summary>
    public static Task<bool> TryComposeArchivesAsync(
        string fusionArchivePath,
        IReadOnlyList<SourceSchemaArchiveInfo> archives,
        GraphQLCompositionSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
        => TryComposeArchivesAsync(
            fusionArchivePath,
            archives,
            settings.EnvironmentName ?? "Aspire",
            settings,
            logger,
            cancellationToken);

    /// <summary>
    /// Composes the given source schema archives into a fusion archive, resolving the source
    /// schema settings against <paramref name="environmentName"/>.
    /// </summary>
    public static async Task<bool> TryComposeArchivesAsync(
        string fusionArchivePath,
        IReadOnlyList<SourceSchemaArchiveInfo> archives,
        string environmentName,
        GraphQLCompositionSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        var sourceSchemas = await ReadSourceSchemasAsync(
            archives,
            cancellationToken);

        try
        {
            using var archive = OpenArchive(fusionArchivePath, seedArchivePath: null);

            return await ComposeArchivesAsync(
                archive,
                sourceSchemas,
                environmentName,
                settings,
                stageSettings: null,
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

    /// <summary>
    /// Composes the given source schema archives into a fusion archive, resolving the source
    /// schema settings against <paramref name="environmentName"/>.
    /// </summary>
    /// <param name="fusionArchive">
    /// The stream that the composed fusion archive is written to.
    /// </param>
    /// <param name="archives">
    /// The source schema archives that are composed.
    /// </param>
    /// <param name="environmentName">
    /// The environment that the settings of the source schemas resolve against.
    /// </param>
    /// <param name="settings">
    /// The composition settings of the gateway.
    /// </param>
    /// <param name="stageSettings">
    /// The composition settings that the deployment target declares. They only fill values that
    /// <paramref name="settings"/> leaves unset.
    /// </param>
    /// <param name="logger">
    /// The logger that receives the composition diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    internal static async Task<bool> TryComposeArchivesAsync(
        Stream fusionArchive,
        IReadOnlyList<SourceSchemaArchiveInfo> archives,
        string environmentName,
        GraphQLCompositionSettings settings,
        CompositionSettings? stageSettings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fusionArchive);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        var sourceSchemas = await ReadSourceSchemasAsync(
            archives,
            cancellationToken);

        try
        {
            using var archive = FusionArchive.Create(
                fusionArchive,
                leaveOpen: true);

            return await ComposeArchivesAsync(
                archive,
                sourceSchemas,
                environmentName,
                settings,
                stageSettings,
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

    /// <summary>
    /// Composes source schemas that were read from archives. They are composed for a deployment,
    /// so the configured URLs are composed as they are, without local overrides and without a
    /// preference for development URLs.
    /// </summary>
    private static Task<bool> ComposeArchivesAsync(
        FusionArchive archive,
        List<SourceSchemaInfo> sourceSchemas,
        string environment,
        GraphQLCompositionSettings settings,
        CompositionSettings? stageSettings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var localSourceSchemas =
            new Dictionary<string, LocalSourceSchema>(sourceSchemas.Count, StringComparer.Ordinal);

        foreach (var sourceSchema in sourceSchemas)
        {
            localSourceSchemas.Add(
                sourceSchema.Name,
                new LocalSourceSchema(
                    sourceSchema.Schema,
                    sourceSchema.SchemaSettings,
                    urlOverride: null));
        }

        return ComposeAsync(
            archive,
            localSourceSchemas,
            environment,
            settings,
            stageSettings,
            preferDevUrls: false,
            logger,
            cancellationToken);
    }

    private static async Task<List<SourceSchemaInfo>> ReadSourceSchemasAsync(
        IReadOnlyList<SourceSchemaArchiveInfo> archives,
        CancellationToken cancellationToken)
    {
        var sourceSchemas = new List<SourceSchemaInfo>(archives.Count);

        try
        {
            foreach (var archiveInfo in archives)
            {
                await using var archiveStream = archiveInfo.OpenRead();
                using var archive = FusionSourceSchemaArchive.Open(
                    archiveStream);
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

            return sourceSchemas;
        }
        catch
        {
            foreach (var sourceSchema in sourceSchemas)
            {
                sourceSchema.SchemaSettings.Dispose();
            }

            throw;
        }
    }

    /// <summary>
    /// Composes the source schemas of the distributed application into a fusion archive. The
    /// composition fails when a source schema declares no GraphQL endpoint path.
    /// </summary>
    /// <param name="fusionArchivePath">
    /// The full path of the fusion archive that is written.
    /// </param>
    /// <param name="seedArchivePath">
    /// The full path of the fusion archive that the composition builds on, which replaces the
    /// content of <paramref name="fusionArchivePath"/>. When it is <c>null</c>, the composition
    /// builds on <paramref name="fusionArchivePath"/> itself.
    /// </param>
    /// <param name="localSourceSchemas">
    /// The source schemas of the distributed application. They replace the source schemas of the
    /// same name that the composition base carries.
    /// </param>
    /// <param name="settings">
    /// The composition settings of the gateway.
    /// </param>
    /// <param name="environment">
    /// The environment that source schema settings resolve against. When it is <c>null</c>, the
    /// obsolete <see cref="GraphQLCompositionSettings.EnvironmentName"/> setting or "Aspire" is
    /// used.
    /// </param>
    /// <param name="logger">
    /// The logger that receives the composition diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public static async Task<bool> TryComposeAsync(
        string fusionArchivePath,
        string? seedArchivePath,
        ImmutableArray<SourceSchemaInfo> localSourceSchemas,
        GraphQLCompositionSettings settings,
        string? environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!TryBuildLocalSourceSchemas(localSourceSchemas, logger, out var sourceSchemas))
        {
            return false;
        }

        using var archive = OpenArchive(fusionArchivePath, seedArchivePath);

        return await ComposeAsync(
            archive,
            sourceSchemas,
            environment ?? settings.EnvironmentName ?? "Aspire",
            settings,
            stageSettings: null,
            preferDevUrls: true,
            logger,
            cancellationToken);
    }

    private static async Task<bool> ComposeAsync(
        FusionArchive archive,
        Dictionary<string, LocalSourceSchema> localSourceSchemas,
        string environment,
        GraphQLCompositionSettings settings,
        CompositionSettings? stageSettings,
        bool preferDevUrls,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        var compositionLog = new CompositionLog();
        var compositionSettings = CreateCompositionSettings(settings, stageSettings);

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            localSourceSchemas,
            archive,
            environment,
            preferDevUrls,
            compositionSettings,
            legacyArchive: null,
            cancellationToken);

        var output = new StringBuilder();

        foreach (var entry in compositionLog)
        {
            if (entry.Severity is LogSeverity.Warning)
            {
                logger.LogWarning("{Message}", entry.Message);
                continue;
            }

            output.AppendLine(entry.Message);
        }

        if (result.IsFailure)
        {
            output.Append("Composition failed:");
            logger.LogError("{Message}", output.ToString());
            return false;
        }

        output.Append("Composition completed successfully.");
        logger.LogInformation("{Message}", output.ToString());

        return true;
    }

    /// <summary>
    /// Opens the archive that the composition writes to. A seed replaces the content of the
    /// archive, so what a previous composition wrote is never an input.
    /// </summary>
    private static FusionArchive OpenArchive(string fusionArchivePath, string? seedArchivePath)
    {
        if (seedArchivePath is not null)
        {
            File.Copy(seedArchivePath, fusionArchivePath, overwrite: true);

            return FusionArchive.Open(fusionArchivePath, FusionArchiveMode.Update);
        }

        return File.Exists(fusionArchivePath)
            ? FusionArchive.Open(fusionArchivePath, FusionArchiveMode.Update)
            : FusionArchive.Create(fusionArchivePath);
    }

    /// <summary>
    /// Builds the local source schemas of the composition, keyed by source schema name. A source
    /// schema that is backed by a resource with an allocated HTTP endpoint carries a URL override
    /// that combines the allocated endpoint origin with the GraphQL path that the resource
    /// declares. Returns <c>false</c> and reports every source schema whose resource declares no
    /// GraphQL path. Duplicate source schema names fail the composition.
    /// </summary>
    internal static bool TryBuildLocalSourceSchemas(
        ImmutableArray<SourceSchemaInfo> sourceSchemas,
        ILogger logger,
        out Dictionary<string, LocalSourceSchema> localSourceSchemas)
    {
        localSourceSchemas = new Dictionary<string, LocalSourceSchema>(StringComparer.Ordinal);
        var success = true;

        foreach (var sourceSchema in sourceSchemas)
        {
            if (sourceSchema.GraphQLPath is not { } graphQLPath)
            {
                ReportMissingGraphQLPath(sourceSchema.Name, sourceSchema.ResourceName, logger);
                success = false;
                continue;
            }

            localSourceSchemas.Add(
                sourceSchema.Name,
                new LocalSourceSchema(
                    sourceSchema.Schema,
                    sourceSchema.SchemaSettings,
                    BuildUrlOverride(sourceSchema, graphQLPath, logger)));
        }

        return success;
    }

    /// <summary>
    /// Reports that the resource backing a source schema is registered with an API that does not
    /// declare the GraphQL endpoint path of the resource. <paramref name="resourceName"/> is
    /// <c>null</c> when the backing resource is unknown.
    /// </summary>
    internal static void ReportMissingGraphQLPath(
        string sourceSchemaName,
        string? resourceName,
        ILogger logger)
    {
        if (resourceName is null)
        {
            logger.LogError(
                "The source schema {Name} does not declare the path of its GraphQL endpoint. "
                + "Call WithGraphQLHttpEndpoint on the resource that serves it.",
                sourceSchemaName);
            return;
        }

        logger.LogError(
            "The source schema {Name} of the resource {ResourceName} does not declare the path "
            + "of its GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource.",
            sourceSchemaName,
            resourceName);
    }

    /// <summary>
    /// Builds the local GraphQL endpoint URL of a source schema from the allocated endpoint origin
    /// and <paramref name="graphQLPath"/>. Returns <c>null</c> when the backing resource has no
    /// allocated HTTP endpoint.
    /// </summary>
    private static Uri? BuildUrlOverride(
        SourceSchemaInfo sourceSchema,
        string graphQLPath,
        ILogger logger)
    {
        if (sourceSchema.AllocatedHttpEndpointUrl is null)
        {
            logger.LogDebug(
                "Source schema {Name} has no allocated HTTP endpoint. No local URL is injected.",
                sourceSchema.Name);
            return null;
        }

        var url = new Uri(
            sourceSchema.AllocatedHttpEndpointUrl.TrimEnd('/') + graphQLPath,
            UriKind.Absolute);

        logger.LogDebug(
            "Injecting local GraphQL endpoint URL {Url} for source schema {Name}.",
            url,
            sourceSchema.Name);

        return url;
    }

    /// <summary>
    /// Resolves the settings of a single source schema against the given environment, so that the
    /// resulting document no longer carries environment specific overrides.
    /// </summary>
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
            environmentName,
            ReadOnlyDictionary<string, Uri>.Empty,
            preferDevUrls: false,
            new CompositionLog());
        using var gatewaySettings = JsonDocument.Parse(buffer.WrittenMemory);
        var sourceSchemas = gatewaySettings.RootElement
            .GetProperty("sourceSchemas");
        var resolvedSettings = sourceSchemas
            .EnumerateObject()
            .Single()
            .Value;

        return JsonSerializer.SerializeToDocument(resolvedSettings);
    }

    /// <summary>
    /// Creates the composition settings of a composition. The settings that the distributed
    /// application declares win, <paramref name="stageSettings"/> only fills the values that they
    /// leave unset.
    /// </summary>
    internal static CompositionSettings CreateCompositionSettings(
        GraphQLCompositionSettings settings,
        CompositionSettings? stageSettings)
    {
        var localSettings = new CompositionSettings
        {
            Merger =
            {
                CacheControlMergeBehavior = settings.CacheControlMergeBehavior,
                EnableGlobalObjectIdentification = settings.EnableGlobalObjectIdentification,
                EnumValuesMergeBehavior = settings.EnumValuesMergeBehavior,
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

        return stageSettings is null
            ? localSettings
            : localSettings.MergeInto(stageSettings);
    }
}

internal readonly record struct SourceSchemaArchiveInfo
{
    private readonly string? _archivePath;
    private readonly byte[]? _archive;

    public SourceSchemaArchiveInfo(string name, string archivePath)
    {
        Name = name;
        _archivePath = archivePath;
    }

    public SourceSchemaArchiveInfo(
        string name,
        byte[] archive)
    {
        Name = name;
        _archive = archive;
    }

    public string Name { get; }

    public Stream OpenRead()
        => _archivePath is null
            ? new MemoryStream(_archive!, writable: false)
            : File.OpenRead(_archivePath);
}

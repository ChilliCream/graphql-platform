using System.Collections.Frozen;
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
    private const string DefaultGraphQLPath = "/graphql";

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
            // the archives are composed for a deployment, so the configured URLs are composed as
            // they are, without local overrides and without a preference for development URLs.
            return await ComposeAsync(
                fusionArchivePath,
                seedArchivePath: null,
                [.. sourceSchemas],
                environmentName,
                settings,
                SettingsComposerOptions.Default,
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

    internal static async Task<bool> TryComposeArchivesAsync(
        Stream fusionArchive,
        IReadOnlyList<SourceSchemaArchiveInfo> archives,
        string environmentName,
        GraphQLCompositionSettings settings,
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
            return await ComposeAsync(
                archive,
                [.. sourceSchemas],
                environmentName,
                settings,
                SettingsComposerOptions.Default,
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
    /// Composes the source schemas of the distributed application into a fusion archive.
    /// </summary>
    /// <param name="fusionArchivePath">
    /// The full path of the fusion archive that is written.
    /// </param>
    /// <param name="seedArchivePath">
    /// The full path of the fusion archive that the composition builds on, which replaces the
    /// content of <paramref name="fusionArchivePath"/>. When it is <c>null</c>, the composition
    /// builds on <paramref name="fusionArchivePath"/> itself.
    /// </param>
    /// <param name="newSourceSchemas">
    /// The source schemas of the distributed application. They replace the source schemas of the
    /// same name that the composition base carries.
    /// </param>
    /// <param name="settings">
    /// The composition settings of the gateway.
    /// </param>
    /// <param name="externalEnvironment">
    /// The environment that the settings of the source schemas which the composition base carries
    /// resolve against. When it is <c>null</c>, every source schema resolves against the
    /// environment of the distributed application.
    /// </param>
    /// <param name="logger">
    /// The logger that receives the composition diagnostics.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public static Task<bool> TryComposeAsync(
        string fusionArchivePath,
        string? seedArchivePath,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        GraphQLCompositionSettings settings,
        string? externalEnvironment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environment = settings.EnvironmentName ?? "Aspire";
        var settingsComposerOptions = new SettingsComposerOptions
        {
            LocalUrlOverrides = BuildLocalUrlOverrides(newSourceSchemas, environment, logger),
            PreferDevUrls = true,
            LocalSourceSchemas = newSourceSchemas
                .Select(s => s.Name)
                .ToFrozenSet(StringComparer.Ordinal),
            ExternalEnvironment = externalEnvironment
        };

        return ComposeAsync(
            fusionArchivePath,
            seedArchivePath,
            newSourceSchemas,
            environment,
            settings,
            settingsComposerOptions,
            logger,
            cancellationToken);
    }

    private static async Task<bool> ComposeAsync(
        string fusionArchivePath,
        string? seedArchivePath,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        string environment,
        GraphQLCompositionSettings settings,
        SettingsComposerOptions settingsComposerOptions,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        using var archive = OpenArchive(fusionArchivePath, seedArchivePath);

        return await ComposeAsync(
            archive,
            newSourceSchemas,
            environment,
            settings,
            settingsComposerOptions,
            logger,
            cancellationToken);
    }

    private static async Task<bool> ComposeAsync(
        FusionArchive archive,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        string environment,
        GraphQLCompositionSettings settings,
        SettingsComposerOptions settingsComposerOptions,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        var compositionLog = new CompositionLog();
        var compositionSettings = CreateCompositionSettings(settings);
        var sourceSchemas = newSourceSchemas.ToDictionary(
            s => s.Name,
            s => (s.Schema, s.SchemaSettings));

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            sourceSchemas,
            archive,
            environment,
            settingsComposerOptions,
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
    /// Builds the local GraphQL endpoint URL per source schema that is backed by a resource
    /// with an allocated HTTP endpoint. The URL uses the allocated endpoint origin combined
    /// with the path of the configured HTTP transport URL, or /graphql when no path can be
    /// determined.
    /// </summary>
    internal static Dictionary<string, string> BuildLocalUrlOverrides(
        ImmutableArray<SourceSchemaInfo> sourceSchemas,
        string environment,
        ILogger logger)
    {
        var localUrlOverrides = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var sourceSchema in sourceSchemas)
        {
            if (sourceSchema.AllocatedHttpEndpointUrl is null)
            {
                logger.LogDebug(
                    "Source schema {Name} has no allocated HTTP endpoint. No local URL is injected.",
                    sourceSchema.Name);
                continue;
            }

            var path = ResolveConfiguredGraphQLPath(sourceSchema.SchemaSettings, environment)
                ?? DefaultGraphQLPath;
            var url = sourceSchema.AllocatedHttpEndpointUrl.TrimEnd('/') + path;

            localUrlOverrides[sourceSchema.Name] = url;

            logger.LogDebug(
                "Injecting local GraphQL endpoint URL {Url} for source schema {Name}.",
                url,
                sourceSchema.Name);
        }

        return localUrlOverrides;
    }

    /// <summary>
    /// Determines the GraphQL endpoint path from the configured HTTP transport URL of a source
    /// schema settings document. Returns <c>null</c> when the settings define no HTTP transport
    /// URL, when the URL contains variables that cannot be resolved for the environment, or
    /// when the URL carries no path.
    /// </summary>
    internal static string? ResolveConfiguredGraphQLPath(
        JsonDocument schemaSettings,
        string environment)
    {
        var root = schemaSettings.RootElement;

        if (root.ValueKind is not JsonValueKind.Object
            || !root.TryGetProperty("transports", out var transports)
            || transports.ValueKind is not JsonValueKind.Object
            || !transports.TryGetProperty("http", out var http)
            || http.ValueKind is not JsonValueKind.Object
            || !http.TryGetProperty("url", out var url)
            || url.ValueKind is not JsonValueKind.String)
        {
            return null;
        }

        if (!SettingsComposer.TryResolveVariables(url.GetString()!, root, environment, out var resolvedUrl)
            || !Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.AbsolutePath.Length <= 1)
        {
            return null;
        }

        return uri.AbsolutePath;
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
            SettingsComposerOptions.Default,
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

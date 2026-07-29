using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class AspireCompositionHelper
{
    private const string DefaultGraphQLPath = "/graphql";

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
    public static async Task<bool> TryComposeAsync(
        string fusionArchivePath,
        string? seedArchivePath,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        GraphQLCompositionSettings settings,
        string? externalEnvironment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var archive = OpenArchive(fusionArchivePath, seedArchivePath);

        var compositionLog = new CompositionLog();
        var environment = settings.EnvironmentName ?? "Aspire";
        var compositionSettings = CreateCompositionSettings(settings);
        var sourceSchemas = newSourceSchemas.ToDictionary(
            s => s.Name,
            s => (s.Schema, s.SchemaSettings));

        var settingsComposerOptions = new SettingsComposerOptions
        {
            LocalUrlOverrides = BuildLocalUrlOverrides(newSourceSchemas, environment, logger),
            PreferDevUrls = true,
            LocalSourceSchemas = sourceSchemas.Keys.ToFrozenSet(StringComparer.Ordinal),
            ExternalEnvironment = externalEnvironment
        };

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

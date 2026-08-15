using System.Collections.Immutable;
using System.Text;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class AspireCompositionHelper
{
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

        var compositionLog = new CompositionLog();
        environment ??= settings.EnvironmentName ?? "Aspire";

        var compositionSettings = CreateCompositionSettings(settings);

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            sourceSchemas,
            archive,
            environment,
            preferDevUrls: true,
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

    internal static CompositionSettings CreateCompositionSettings(
        GraphQLCompositionSettings settings)
    {
        return new CompositionSettings
        {
            Merger =
            {
                CacheControlMergeBehavior = settings.CacheControlMergeBehavior,
                EnableGlobalObjectIdentification = settings.EnableGlobalObjectIdentification,
                EnumValuesMergeBehavior = settings.EnumValuesMergeBehavior,
                NodeResolution = settings.NodeResolution,
                TagMergeBehavior = settings.TagMergeBehavior
            },
            Satisfiability =
            {
                IncludeSatisfiabilityPaths = settings.IncludeSatisfiabilityPaths
            },
            ApolloFederationCompatibility =
            {
                AllowNonResolvableInterfaceObjects = settings.AllowNonResolvableInterfaceObjects,
                ShareableFieldRuntimeTypeRouting = settings.ShareableFieldRuntimeTypeRouting
            },
            Preprocessor =
            {
                ExcludeByTag = settings.ExcludeByTag?.ToHashSet()
            }
        };
    }
}

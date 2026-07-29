using System.Collections.Immutable;
using System.Text;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class AspireCompositionHelper
{
    public static async Task<bool> TryComposeAsync(
        string fusionArchivePath,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        GraphQLCompositionSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var archive = File.Exists(fusionArchivePath)
            ? FusionArchive.Open(fusionArchivePath, FusionArchiveMode.Update)
            : FusionArchive.Create(fusionArchivePath);

        var compositionLog = new CompositionLog();
        var environment = settings.EnvironmentName ?? "Aspire";
        var compositionSettings = CreateCompositionSettings(settings);
        var sourceSchemas = newSourceSchemas.ToDictionary(
            s => s.Name,
            s => (s.Schema, s.SchemaSettings));

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            sourceSchemas,
            archive,
            environment,
            compositionSettings,
            legacyArchive: null,
            cancellationToken);

        var output = new StringBuilder();

        foreach (var entry in compositionLog)
        {
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

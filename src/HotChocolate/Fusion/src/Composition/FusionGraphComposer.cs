using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Composition.Pipeline;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Composes subgraph schemas into a single,
/// merged schema representing the fusion gateway configuration.
/// </summary>
public sealed class FusionGraphComposer
{
    private readonly string? _fusionTypePrefix;
    private readonly bool _fusionTypeSelf;
    private readonly MergeDelegate _pipeline;
    private readonly Func<ICompositionLog>? _logFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FusionGraphComposer"/> class.
    /// </summary>
    /// <param name="fusionTypePrefix">
    /// The prefix that is used for the fusion types.
    /// </param>
    /// <param name="fusionTypeSelf">
    /// Defines if the fusion types should be prefixed with the subgraph name.
    /// </param>
    /// <param name="logFactory">
    /// A factory that creates a new composition log.
    /// </param>
    public FusionGraphComposer(
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false,
        Func<ICompositionLog>? logFactory = null)
        : this(
            [
                new LookupEntityEnricher(),
                new RefResolverEntityEnricher(),
                new PatternEntityEnricher(),
                new RequireEnricher(),
                new NodeEntityEnricher(),
            ],
            [
                new InterfaceTypeMergeHandler(), new UnionTypeMergeHandler(),
                new InputObjectTypeMergeHandler(), new EnumTypeMergeHandler(),
                new ScalarTypeMergeHandler()
            ],
            fusionTypePrefix,
            fusionTypeSelf,
            logFactory)
    { }

    internal FusionGraphComposer(
        IEnumerable<IEntityEnricher> entityEnrichers,
        IEnumerable<ITypeMergeHandler> mergeHandlers,
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false,
        Func<ICompositionLog>? logFactory = null)
    {
        // Build the merge pipeline with the given entity enrichers and merge handlers.
        _pipeline =
            MergePipelineBuilder.New()
                .Use<ParseSubgraphSchemaMiddleware>()
                .Use<RegisterClientsMiddleware>()
                .Use<ApplyRenameDirectiveMiddleware>()
                .Use<ApplyRemoveDirectiveMiddleware>()
                .Use(() => new EnrichEntityMiddleware(entityEnrichers))
                .Use<PrepareFusionSchemaMiddleware>()
                .Use<MergeEntityMiddleware>()
                .Use<EntityFieldDependencyMiddleware>()
                .Use(() => new MergeTypeMiddleware(mergeHandlers))
                .Use<RemoveDirectivesWithoutLocationMiddleware>()
                .Use<MergeQueryAndMutationTypeMiddleware>()
                .Use<MergeSchemaDefinitionMiddleware>()
                .Use<MergeSubscriptionTypeMiddleware>()
                .Use<NodeMiddleware>()
                .Use<ApplyTagDirectiveMiddleware>()
                .Use<ApplyExcludeTagMiddleware>()
                .Use<RemoveFusionTypesMiddleware>()
                .Build();
        _logFactory = logFactory;
        _fusionTypePrefix = fusionTypePrefix;
        _fusionTypeSelf = fusionTypeSelf;
    }

    /// <summary>
    /// Composes the subgraph schemas into a single,
    /// merged schema representing the fusion gateway configuration.
    /// </summary>
    /// <param name="configurations">
    /// The subgraph configurations to compose.
    /// </param>
    /// <param name="features">
    /// The composition feature flags.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>The fusion gateway configuration.</returns>
    public async ValueTask<SchemaDefinition> ComposeAsync(
        IEnumerable<SubgraphConfiguration> configurations,
        FusionFeatureCollection? features = null,
        CancellationToken cancellationToken = default)
    {
        var log = new DefaultCompositionLog(_logFactory?.Invoke());

        // Create a new composition context with the given subgraph configurations,
        // fusion type prefix, and fusion type self option.
        var context = new CompositionContext(
            configurations.ToArray(),
            features ?? FusionFeatureCollection.Empty,
            log,
            _fusionTypePrefix,
            _fusionTypeSelf)
        {
            Abort = cancellationToken,
        };

        // Run the merge pipeline on the composition context.
        await _pipeline(context);

        if (log.HasErrors)
        {
            throw new CompositionException(
                log.Where(t => t.Severity == LogSeverity.Error).ToArray());
        }

        // Return the resulting merged schema.
        return context.FusionGraph;
    }

    /// <summary>
    /// Composes the subgraph schemas into a single,
    /// merged schema representing the fusion gateway configuration.
    /// </summary>
    /// <param name="configurations">
    /// The subgraph configurations to compose.
    /// </param>
    /// <param name="features">
    /// The composition feature flags.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>The fusion gateway configuration.</returns>
    public async ValueTask<SchemaDefinition?> TryComposeAsync(
        IEnumerable<SubgraphConfiguration> configurations,
        FusionFeatureCollection? features = null,
        CancellationToken cancellationToken = default)
    {
        var log = new DefaultCompositionLog(_logFactory?.Invoke());

        // Create a new composition context with the given subgraph configurations,
        // fusion type prefix, and fusion type self option.
        var context = new CompositionContext(
            configurations.ToArray(),
            features ?? FusionFeatureCollection.Empty,
            log,
            _fusionTypePrefix,
            _fusionTypeSelf)
        {
            Abort = cancellationToken,
        };

        // Run the merge pipeline on the composition context.
        await _pipeline(context);

        return log.HasErrors ? null : context.FusionGraph;
    }

    /// <summary>
    /// Composes the subgraph schemas into a single,
    /// merged schema representing the fusion gateway configuration.
    /// </summary>
    /// <param name="configurations">
    /// The subgraph configurations to compose.
    /// </param>
    /// <param name="features">
    /// The composition feature flags.
    /// </param>
    /// <param name="fusionTypePrefix">
    /// The prefix that is used for the fusion types.
    /// </param>
    /// <param name="fusionTypeSelf">
    /// Defines if the fusion types should be prefixed with the subgraph name.
    /// </param>
    /// <param name="logFactory">
    /// A factory that creates a new composition log.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>The fusion gateway configuration.</returns>
    public static async ValueTask<SchemaDefinition> ComposeAsync(
        IEnumerable<SubgraphConfiguration> configurations,
        FusionFeatureCollection? features = null,
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false,
        Func<ICompositionLog>? logFactory = null,
        CancellationToken cancellationToken = default)
    {
        var composer = new FusionGraphComposer(
            fusionTypePrefix,
            fusionTypeSelf,
            logFactory);

        return await composer.ComposeAsync(
            configurations,
            features,
            cancellationToken);
    }
}

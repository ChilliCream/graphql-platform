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

    /// <summary>
    /// Initializes a new instance of the <see cref="FusionGraphComposer"/> class.
    /// </summary>
    /// <param name="fusionTypePrefix">
    /// The prefix that is used for the fusion types.
    /// </param>
    /// <param name="fusionTypeSelf">
    /// Defines if the fusion types should be prefixed with the subgraph name.
    /// </param>
    public FusionGraphComposer(
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false)
        : this(
            new IEntityEnricher[] { new RefResolverEntityEnricher() },
            new ITypeMergeHandler[]
            {
                new InterfaceTypeMergeHandler(), new UnionTypeMergeHandler(),
                new InputObjectTypeMergeHandler(), new EnumTypeMergeHandler(),
                new ScalarTypeMergeHandler()
            },
            fusionTypePrefix,
            fusionTypeSelf) { }

    internal FusionGraphComposer(
        IEnumerable<IEntityEnricher> entityEnrichers,
        IEnumerable<ITypeMergeHandler> mergeHandlers,
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false)
    {
        // Build the merge pipeline with the given entity enrichers and merge handlers.
        _pipeline =
            MergePipelineBuilder.New()
                .Use<ParseSubgraphSchemaMiddleware>()
                .Use<ApplyRenameDirectiveMiddleware>()
                .Use<ApplyRemoveDirectiveMiddleware>()
                .Use(() => new EnrichEntityMiddleware(entityEnrichers))
                .Use<PrepareFusionSchemaMiddleware>()
                .Use<MergeEntityMiddleware>()
                .Use(() => new MergeTypeMiddleware(mergeHandlers))
                .Use<MergeQueryTypeMiddleware>()
                .Build();

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
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>The fusion gateway configuration.</returns>
    public async ValueTask<Schema> ComposeAsync(
        IEnumerable<SubgraphConfiguration> configurations,
        CancellationToken cancellationToken = default)
    {
        // Create a new composition context with the given subgraph configurations,
        // fusion type prefix, and fusion type self option.
        var context = new CompositionContext(
            configurations.ToArray(),
            _fusionTypePrefix,
            _fusionTypeSelf)
        {
            Abort = cancellationToken
        };

        // Run the merge pipeline on the composition context.
        await _pipeline(context);

        // Return the resulting merged schema.
        return context.FusionGraph;
    }
}

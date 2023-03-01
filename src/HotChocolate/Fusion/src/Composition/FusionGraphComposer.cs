namespace HotChocolate.Fusion.Composition;

public sealed class FusionGraphComposer
{
    private readonly MergeDelegate _pipeline;

    public FusionGraphComposer(
        IEnumerable<IEntityEnricher> entityEnrichers,
        IEnumerable<ITypeMergeHandler> mergeHandlers)
    {
        _pipeline =
            MergePipelineBuilder.New()
                .Use<ParseSubGraphSchemaMiddleware>()
                .Use<ApplyRenameDirectiveMiddleware>()
                .Use<ApplyRemoveDirectiveMiddleware>()
                .Use(() => new EnrichEntityMiddleware(entityEnrichers))
                .Use<PrepareFusionSchemaMiddleware>()
                .Use<MergeEntityMiddleware>()
                .Use(() => new MergeTypeMiddleware(mergeHandlers))
                .Use<MergeQueryTypeMiddleware>()
                .Build();
    }

    public async ValueTask<CompositionContext> ComposeAsync(
        params SubGraphConfiguration[] configurations)
    {
        var context = new CompositionContext(configurations);
        await _pipeline(context);
        return context;
    }
}

namespace HotChocolate.Fusion.Composition;

public sealed class FusionGraphComposer
{
    private readonly MergeDelegate _pipeline;

    public FusionGraphComposer(IEnumerable<IEntityEnricher> enrichers)
    {
        _pipeline =
            MergePipelineBuilder.New()
                .Use<ParseSubGraphSchemaMiddleware>()
                .Use<ApplyRenameDirectiveMiddleware>()
                .Use<ApplyRemoveDirectiveMiddleware>()
                .Use(() => new EnrichEntityMiddleware(enrichers))
                .Use<PrepareFusionSchemaMiddleware>()
                .Use<MergeEntityMiddleware>()
                .Use<MergeTypeMiddleware>()
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

using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed class FusionGraphComposer
{
    private readonly MergeDelegate _pipeline;

    public FusionGraphComposer(IEnumerable<IObjectTypeMetaDataEnricher> enrichers)
    {
        _pipeline =
            MergePipelineBuilder.New()
                .Use<ParseSubGraphSchemaMiddleware>()
                .Use<ApplyRemoveDirectiveMiddleware>()
                .Use<ApplyRemoveDirectiveMiddleware>()
                .Use(() => new EnrichObjectTypesMiddleware(enrichers))
                .Build();
    }

    public async ValueTask<Schema> ComposeAsync(params SubGraphConfiguration[] configurations)
    {
        var context = new CompositionContext(configurations);
        await _pipeline(context);
        return context.FusionGraph;
    }
}

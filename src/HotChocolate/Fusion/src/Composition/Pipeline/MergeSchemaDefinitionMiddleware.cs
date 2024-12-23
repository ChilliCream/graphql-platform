namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeSchemaDefinitionMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var target = context.FusionGraph;

        foreach (var schema in context.Subgraphs)
        {
            target.MergeDirectivesWith(schema, context);
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class RemoveDirectivesWithoutLocationMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach(var directive in context.FusionGraph.DirectiveDefinitions)
        {
            if (context.FusionTypes.IsFusionDirective(directive.Name))
            {
                continue;
            }

            if (directive.Locations == 0)
            {
                context.FusionGraph.DirectiveDefinitions.Remove(directive);
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

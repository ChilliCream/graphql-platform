using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class RemoveDirectivesWithoutLocationMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var removeDirectives = new List<DirectiveDefinition>();

        foreach(var directive in context.FusionGraph.DirectiveDefinitions)
        {
            if (context.FusionTypes.IsFusionDirective(directive.Name))
            {
                continue;
            }

            if (directive.Locations == 0)
            {
                removeDirectives.Add(directive);
            }
        }

        foreach (var directive in removeDirectives)
        {
            context.FusionGraph.DirectiveDefinitions.Remove(directive);
        }

        removeDirectives.Clear();

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

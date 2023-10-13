using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.LogEntryHelper;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// This composition middleware will apply the @remove directives to the subgraph
/// and remove type system member that are not wanted in the fusion schema.
/// </summary>
internal sealed class ApplyRemoveDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.Subgraphs)
        {
            foreach (var directive in RemoveDirective.GetAllFrom(schema, context.FusionTypes))
            {
                if (!schema.RemoveMember(directive.Coordinate))
                {
                    context.Log.Write(RemoveMemberNotFound(directive.Coordinate, schema));
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.LogEntryHelper;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// This composition middleware will apply the @remove directives to the
/// schema and remove type system member that are not wanted in the fusion schema.
/// </summary>
public sealed class ApplyRemoveDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.SubGraphs)
        {
            foreach (var directive in schema.GetRemoveDirectives())
            {
                if (!schema.RemoveMember(directive.Coordinate))
                {
                    context.Log.Warning(RemoveMemberNotFound(directive.Coordinate, schema));
                }
            }
        }

        await next(context).ConfigureAwait(false);
    }
}

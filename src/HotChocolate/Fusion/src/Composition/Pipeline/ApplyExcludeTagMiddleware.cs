using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ApplyExcludeTagMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var tagContext = context.GetTagContext();
        var schema = context.FusionGraph;

        foreach (var excludedTag in context.Features.GetExcludedTags())
        {
            foreach (var coordinate in tagContext.GetTagCoordinates(excludedTag))
            {
                if (!schema.RemoveMember(coordinate, onRequiredRemoveParent: true))
                {
                    context.Log.Write(LogEntryHelper.RemoveMemberNotFound(coordinate, schema));
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }
}

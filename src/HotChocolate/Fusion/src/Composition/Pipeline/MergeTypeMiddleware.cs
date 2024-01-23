using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeTypeMiddleware : IMergeMiddleware
{
    private readonly ITypeMergeHandler[] _mergeHandlers;

    public MergeTypeMiddleware(IEnumerable<ITypeMergeHandler> mergeHandlers)
    {
        if (mergeHandlers is null)
        {
            throw new ArgumentNullException(nameof(mergeHandlers));
        }

        _mergeHandlers = mergeHandlers.ToArray();
    }

    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var groupedTypes = new Dictionary<string, List<TypePart>>();

        foreach (var schema in context.Subgraphs)
        {
            foreach (var type in schema.Types)
            {
                if (!groupedTypes.TryGetValue(type.Name, out var types))
                {
                    types = [];
                    groupedTypes.Add(type.Name, types);
                }
                types.Add(new TypePart(type, schema));
            }
        }

        foreach (var types in groupedTypes)
        {
            var typeGroup = new TypeGroup(types.Key, types.Value);
            var status = MergeStatus.Skipped;

            // Entity type groups are handled in a separate middleware and we
            // will just skip those here.
            if (types.Value.All(t => t.Type.Kind is TypeKind.Object))
            {
                continue;
            }

            foreach (var handler in _mergeHandlers)
            {
                status = await handler.MergeAsync(context, typeGroup, context.Abort)
                    .ConfigureAwait(false);

                if (status is MergeStatus.Completed)
                {
                    break;
                }
            }

            // If no merge handler was able to merge the type group we will log an error
            // so that the pipeline can complete with an error state that
            // must be handled by the user.
            if (status is MergeStatus.Skipped)
            {
                context.Log.Write(LogEntryHelper.UnableToMergeType(typeGroup));
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

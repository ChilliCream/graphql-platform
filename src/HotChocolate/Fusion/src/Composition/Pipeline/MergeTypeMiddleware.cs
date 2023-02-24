
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed class MergeTypeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var groupedTypes = new Dictionary<string, List<INamedType>>();

        foreach (var schema in context.SubGraphs)
        {
            foreach (var type in schema.Types)
            {
                if (!groupedTypes.TryGetValue(type.Name, out var types))
                {
                    types = new List<INamedType>();
                    groupedTypes.Add(type.Name, types);
                }
                types.Add(type);
            }
        }

        foreach (var types in groupedTypes)
        {

        }

        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }
}

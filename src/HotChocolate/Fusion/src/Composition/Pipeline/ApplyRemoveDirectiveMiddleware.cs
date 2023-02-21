using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

public sealed class ApplyRemoveDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.SubGraphs)
        {
            foreach (var directive in schema.Directives["remove"])
            {
                var coordinate = directive.Arguments.FirstOrDefault(
                    t => t.Name.EqualsOrdinal("coordinate"));

                if (coordinate?.Value is not StringValueNode coordinateValue)
                {
                    // TODO : FIX IT
                    throw new Exception("");
                }

                if (!schema.RemoveMember(SchemaCoordinate.Parse(coordinateValue.Value)))
                {
                    // TODO : FIX IT
                    throw new Exception("");
                }
            }
        }

        await next(context).ConfigureAwait(false);
    }
}

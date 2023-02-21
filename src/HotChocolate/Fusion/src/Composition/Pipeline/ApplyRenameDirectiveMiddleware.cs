using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

public sealed class ApplyRenameDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.SubGraphs)
        {
            foreach (var directive in schema.Directives["rename"])
            {
                var coordinateArg = directive.Arguments.FirstOrDefault(
                    t => t.Name.EqualsOrdinal("coordinate"));

                var toArg = directive.Arguments.FirstOrDefault(
                    t => t.Name.EqualsOrdinal("to"));

                if (coordinateArg?.Value is not StringValueNode coordinate)
                {
                    // TODO : FIX IT
                    throw new Exception("");
                }

                if (toArg?.Value is not StringValueNode to)
                {
                    // TODO : FIX IT
                    throw new Exception("");
                }

                if (!schema.RenameMember(SchemaCoordinate.Parse(coordinate.Value), to.Value))
                {
                    // TODO : FIX IT
                    throw new Exception("");
                }
            }
        }

        await next(context).ConfigureAwait(false);
    }
}

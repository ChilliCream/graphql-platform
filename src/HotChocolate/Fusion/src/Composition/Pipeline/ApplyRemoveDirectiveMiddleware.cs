using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.DirectivesHelper;
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
            foreach (var directive in schema.GetRemoveDirectives(context))
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

file static class ApplyRemoveDirectiveMiddlewareExtensions
{
    public static IEnumerable<RemoveDirective> GetRemoveDirectives(
        this SchemaDefinition schema,
        CompositionContext context)
    {
        foreach (var directive in schema.Directives[RemoveDirectiveName])
        {
            if (!directive.Arguments.TryGetValue(CoordinateArg, out var argumentValue))
            {
                context.Log.Write(DirectiveArgumentMissing(CoordinateArg, directive, schema));
                continue;
            }

            if (argumentValue is not StringValueNode coordinateValue ||
                !SchemaCoordinate.TryParse(coordinateValue.Value, out var coordinate))
            {
                context.Log.Write(DirectiveArgumentValueInvalid(CoordinateArg, directive, schema));
                continue;
            }

            yield return new RemoveDirective(coordinate.Value);
        }
    }
}

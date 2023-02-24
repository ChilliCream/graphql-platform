using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.DirectivesHelper;
using static HotChocolate.Fusion.Composition.LogEntryHelper;

namespace HotChocolate.Fusion.Composition;

public sealed class ApplyRenameDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.SubGraphs)
        {
            foreach (var directive in schema.GetRenameDirectives(context))
            {
                if (!schema.RenameMember(directive.Coordinate, directive.NewName))
                {
                    context.Log.Warning(RenameMemberNotFound(directive.Coordinate, schema));
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

static file class ApplyRenameDirectiveMiddlewareExtensions
{
    public static IEnumerable<RenameDirective> GetRenameDirectives(
        this Schema schema,
        CompositionContext context)
    {
        foreach (var directive in schema.Directives[RenameDirectiveName])
        {
            if (!directive.Arguments.TryGetValue(CoordinateArg, out var argumentValue))
            {
                context.Log.Error(DirectiveArgumentMissing(CoordinateArg, directive, schema));
                continue;
            }

            if (argumentValue is not StringValueNode coordinateValue ||
                !SchemaCoordinate.TryParse(coordinateValue.Value, out var coordinate))
            {
                context.Log.Error(DirectiveArgumentValueInvalid(CoordinateArg, directive, schema));
                continue;
            }

            if (!directive.Arguments.TryGetValue(NewNameArg, out argumentValue))
            {
                context.Log.Error(DirectiveArgumentMissing(NewNameArg, directive, schema));
                continue;
            }

            if (argumentValue is not StringValueNode { Value: { Length: > 0 } newName })
            {
                context.Log.Error(DirectiveArgumentValueInvalid(NewNameArg, directive, schema));
                continue;
            }

            yield return new RenameDirective(coordinate.Value,newName);
        }
    }
}

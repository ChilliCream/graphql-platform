using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.DirectivesHelper;
using static HotChocolate.Fusion.Composition.LogEntryHelper;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// This composition middleware will apply the @rename directives to subgraphs
/// and rename type system member.
/// </summary>
internal sealed class ApplyRenameDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.Subgraphs)
        {
            foreach (var directive in schema.GetRenameDirectives(context))
            {
                if (schema.TryGetMember(directive.Coordinate, out INameProvider? member) &&
                    member is IFeatureProvider memberWithFeatures)
                {
                    memberWithFeatures.SetOriginalName(member.Name);
                }

                if (!schema.RenameMember(directive.Coordinate, directive.NewName))
                {
                    context.Log.Write(RenameMemberNotFound(directive.Coordinate, schema));
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

file static class ApplyRenameDirectiveMiddlewareExtensions
{
    public static IEnumerable<RenameDirective> GetRenameDirectives(
        this SchemaDefinition schema,
        CompositionContext context)
    {
        foreach (var directive in schema.Directives[RenameDirectiveName])
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

            if (!directive.Arguments.TryGetValue(NewNameArg, out argumentValue))
            {
                context.Log.Write(DirectiveArgumentMissing(NewNameArg, directive, schema));
                continue;
            }

            if (argumentValue is not StringValueNode { Value: { Length: > 0, } newName, })
            {
                context.Log.Write(DirectiveArgumentValueInvalid(NewNameArg, directive, schema));
                continue;
            }

            yield return new RenameDirective(coordinate.Value, newName);
        }
    }
}

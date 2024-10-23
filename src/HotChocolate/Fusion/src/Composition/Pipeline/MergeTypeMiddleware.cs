using HotChocolate.Skimmed;
using HotChocolate.Types;

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
        var groupedDirectives = new Dictionary<string, List<DirectiveDefinition>>();

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

            foreach (var directiveDefinition in schema.DirectiveDefinitions)
            {
                if (!groupedDirectives.TryGetValue(directiveDefinition.Name, out var directiveDefinitions))
                {
                    directiveDefinitions = [];
                    groupedDirectives.Add(directiveDefinition.Name, directiveDefinitions);
                }
                directiveDefinitions.Add(directiveDefinition);
            }
        }

        foreach (var (directiveName, directiveDefinitions) in groupedDirectives)
        {
            if (context.FusionTypes.IsFusionDirective(directiveName)
                || BuiltIns.IsBuiltInDirective(directiveName)
                // @tag is handled separately
                || directiveName == "tag")
            {
                continue;
            }

            var target = context.FusionGraph.DirectiveDefinitions[directiveName];

            foreach (var directiveDefinition in directiveDefinitions)
            {
                var source = directiveDefinition;

                MergeDirectiveDefinition(source, target, context);
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

    private static void MergeDirectiveDefinition(
        DirectiveDefinition source,
        DirectiveDefinition target,
        CompositionContext context)
    {
        if (!target.IsRepeatable)
        {
            target.IsRepeatable = source.IsRepeatable;
        }

        foreach (var sourceArgument in source.Arguments)
        {
            if (!target.Arguments.TryGetField(sourceArgument.Name, out var targetArgument))
            {
                context.Log.Write(LogEntryHelper.DirectiveDefinitionArgumentMismatch(new SchemaCoordinate(source.Name), source));
                continue;
            }

            if (!sourceArgument.Type.Equals(targetArgument.Type, TypeComparison.Structural))
            {
                context.Log.Write(LogEntryHelper.DirectiveDefinitionArgumentMismatch(new SchemaCoordinate(source.Name), source));
            }
        }

        // Directive definitions without a location will be removed by RemoveDirectivesWithoutLocationMiddleware
        // in a later stage.
        target.Locations = RemoveExecutableLocations(source.Locations & target.Locations);

        target.MergeDescriptionWith(source);
    }

    private static DirectiveLocation RemoveExecutableLocations(DirectiveLocation location)
    {
        return location
            & ~DirectiveLocation.Query
            & ~DirectiveLocation.Mutation
            & ~DirectiveLocation.Subscription
            & ~DirectiveLocation.Field
            & ~DirectiveLocation.FragmentDefinition
            & ~DirectiveLocation.FragmentSpread
            & ~DirectiveLocation.InlineFragment
            & ~DirectiveLocation.VariableDefinition;
    }
}

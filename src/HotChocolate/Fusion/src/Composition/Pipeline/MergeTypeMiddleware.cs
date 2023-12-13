using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeTypeMiddleware : IMergeMiddleware
{
    private readonly ITypeMergeHandler[] _mergeHandlers;
    private readonly QueryTypeMergeHandler _queryTypeMergeHandler = new();

    public MergeTypeMiddleware(IEnumerable<ITypeMergeHandler> mergeHandlers)
    {
        ArgumentNullException.ThrowIfNull(mergeHandlers);
        _mergeHandlers = mergeHandlers.ToArray();
    }

    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var rootTypes = new Dictionary<OperationType, List<TypePart>>();
        var groupedTypes = new Dictionary<string, List<TypePart>>();
        
        foreach (var schema in context.Subgraphs)
        {
            foreach (var type in schema.Types)
            {
                GroupType(type, schema, rootTypes, groupedTypes);
            }
        }

        MergeQueryTypes(context, rootTypes);
        MergeTypes(context, groupedTypes);

        var rewriter = new EnsureTypesAreConsistent(context.FusionGraph);
        rewriter.VisitSchema(context.FusionGraph, null);

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static void GroupType(
        INamedType type,
        Schema schema,
        Dictionary<OperationType, List<TypePart>> rootTypes, 
        Dictionary<string, List<TypePart>> groupedTypes)
    {
        if (ReferenceEquals(schema.QueryType, type))
        {
            if (!rootTypes.TryGetValue(OperationType.Query, out var types))
            {
                types = new List<TypePart>();
                rootTypes.Add(OperationType.Query, types);
            }
            types.Add(new TypePart(type, schema));
        }
        else if (ReferenceEquals(schema.MutationType, type))
        {
            if (!rootTypes.TryGetValue(OperationType.Mutation, out var types))
            {
                types = new List<TypePart>();
                rootTypes.Add(OperationType.Mutation, types);
            }
            types.Add(new TypePart(type, schema));
        }
        else if (ReferenceEquals(schema.SubscriptionType, type))
        {
            if (!rootTypes.TryGetValue(OperationType.Subscription, out var types))
            {
                types = new List<TypePart>();
                rootTypes.Add(OperationType.Subscription, types);
            }
            types.Add(new TypePart(type, schema));
        }
        else
        {
            if (!groupedTypes.TryGetValue(type.Name, out var types))
            {
                types = new List<TypePart>();
                groupedTypes.Add(type.Name, types);
            }
            types.Add(new TypePart(type, schema));
        }
    }
    
    private void MergeQueryTypes(
        CompositionContext context,
        Dictionary<OperationType, List<TypePart>> groupedTypes)
    {
        if (groupedTypes.TryGetValue(OperationType.Query, out var types))
        {
            var typeName = types.First().Type.Name;
            var typeGroup = new TypeGroup(typeName, types);
            _queryTypeMergeHandler.Merge(context, typeGroup);
        }
    }

    private void MergeTypes(
        CompositionContext context,
        Dictionary<string, List<TypePart>> groupedTypes)
    {
        foreach (var types in groupedTypes)
        {
            var typeGroup = new TypeGroup(types.Key, types.Value);
            var status = MergeStatus.Skipped;

            foreach (var handler in _mergeHandlers)
            {
                if (handler.Kind != typeGroup.Kind)
                {
                    continue;
                }

                status = handler.Merge(context, typeGroup);

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
    }

    private class EnsureTypesAreConsistent(Schema fusionGraph) : SchemaVisitor<object?>
    {
        public override void VisitOutputField(OutputField field, object? context)
        {
            var source = field.Type.NamedType();

            switch (source)
            {
                case MissingType:
                {
                    if (fusionGraph.Types.TryGetType(source.Name, out var target))
                    {
                        field.Type = field.Type.ReplaceNameType(_ => target);
                    }
                }
                    break;

                default:
                {
                    if (fusionGraph.Types.TryGetType(source.Name, out var target) &&
                        !ReferenceEquals(source, target))
                    {
                        field.Type = field.Type.ReplaceNameType(_ => target);
                    }
                }
                    break;
            }
        }

        public override void VisitInputField(InputField field, object? context)
        {
            var source = field.Type.NamedType();

            switch (source)
            {
                case MissingType:
                {
                    if (fusionGraph.Types.TryGetType(source.Name, out var target))
                    {
                        field.Type = field.Type.ReplaceNameType(_ => target);
                    }
                    break;
                }

                default:
                {
                    if (fusionGraph.Types.TryGetType(source.Name, out var target) &&
                        !ReferenceEquals(source, target))
                    {
                        field.Type = field.Type.ReplaceNameType(_ => target);
                    }
                    break;
                }
            }
        }
    }
}
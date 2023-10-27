using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeTypeMiddleware : IMergeMiddleware
{
    private readonly ITypeMergeHandler[] _mergeHandlers;

    public MergeTypeMiddleware(IEnumerable<ITypeMergeHandler> mergeHandlers)
    {
        ArgumentNullException.ThrowIfNull(mergeHandlers);
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
                    types = new List<TypePart>();
                    groupedTypes.Add(type.Name, types);
                }
                types.Add(new TypePart(type, schema));
            }
        }

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

        var rewriter = new EnsureTypesAreConsistent(context.FusionGraph);
        rewriter.VisitSchema(context.FusionGraph, null);

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
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
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ApplyEntityResolvers : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var fieldContext = new FieldContext(context, new(), new());
        var variables = new HashSet<VariableDirective>();

        foreach (var group in context.EntityResolverInfos.GroupBy(t => t.EntityName))
        {
            if (!context.FusionGraph.Types.TryGetType<ObjectType>(group.Key, out var type))
            {
                // TODO: log error
                continue;
            }

            foreach (var entityResolver in group)
            {
                foreach (var sourceArgument in entityResolver.SourceArguments)
                {
                    foreach (var variable in CreateVariables(fieldContext, type, sourceArgument))
                    {
                        variables.Add(variable);
                    }
                }

                switch (entityResolver.Kind)
                {
                    case ResolverKind.Fetch:
                        break;

                    case ResolverKind.Batch:
                        break;

                    case ResolverKind.Subscribe:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var variable in variables.OrderBy(t => t.Name).ThenBy(t => t.Subgraph))
            {
                type.Directives.Add(variable.ToDirective(context.FusionTypes));
            }
        }


        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }

    private static IEnumerable<VariableDirective> CreateVariables(
        FieldContext context,
        ObjectType typeContext,
        EntitySourceArgument sourceArgument)
    {
        context.Clear();

        DetermineSubgraphs(context, typeContext, sourceArgument.Directive.Field!);

        foreach (var subgraph in context.Subgraphs)
        {
            yield return new VariableDirective(
                typeContext.CreateVariableName(sourceArgument.Directive),
                sourceArgument.Directive.Field!,
                subgraph);
        }
    }

    private static void DetermineSubgraphs(
        FieldContext context,
        INamedType typeContext,
        ISelectionNode selection)
    {
        // note: we need to refine this... at the moment input objects are problematic as we do not have
        // a marker to indicate that we are in an input object.

        // note: we need to create errors when something cannot be handled.

        context.FieldSubgraphs.Clear();

        switch (selection)
        {
            case FieldNode fieldNode:
                if (TryGetField(typeContext, fieldNode.Name.Value, out var field))
                {
                    foreach (var source in SourceDirective.GetAllFrom(field, context.FusionTypes))
                    {
                        context.FieldSubgraphs.Add(source.Subgraph);
                    }

                    context.Subgraphs.IntersectWith(context.FieldSubgraphs);

                    if (fieldNode.SelectionSet is { } selectionSet)
                    {
                        foreach (var child in selectionSet.Selections)
                        {
                            DetermineSubgraphs(context, typeContext, child);
                        }
                    }
                }
                break;

            case InlineFragmentNode inlineFragmentNode:
                if (IsAssignable(context, inlineFragmentNode.TypeCondition, typeContext))
                {
                    foreach (var child in inlineFragmentNode.SelectionSet.Selections)
                    {
                        DetermineSubgraphs(context, typeContext, child);
                    }
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(selection));
        }
    }

    private static bool TryGetField(
        INamedType type,
        string fieldName,
        [NotNullWhen(true)] out OutputField? field)
    {
        switch (type)
        {
            case InterfaceType interfaceType:
                if (interfaceType.Fields.TryGetField(fieldName, out field))
                {
                    return true;
                }
                break;

            case ObjectType objectType:
                if (objectType.Fields.TryGetField(fieldName, out field))
                {
                    return true;
                }
                break;

            default:
                field = null;
                return false;
        }

        field = null;
        return false;
    }

    private static bool IsAssignable(FieldContext context, NamedTypeNode? typeCondition, INamedType type)
    {
        if (typeCondition is null)
        {
            return true;
        }

        // todo: we should have an error here
        return context.FusionGraph.Types.TryGetType(typeCondition.Name.Value, out var typeConditionType) &&
            typeConditionType.IsAssignableFrom(type);
    }

    private readonly struct FieldContext(
        CompositionContext context,
        HashSet<string> subgraphs,
        HashSet<string> fieldSubgraphs)
    {
        public FusionTypes FusionTypes => context.FusionTypes;

        public Schema FusionGraph => context.FusionGraph;

        public HashSet<string> Subgraphs { get; } = subgraphs;

        public HashSet<string> FieldSubgraphs { get; } = fieldSubgraphs;

        public void Clear()
        {
            Subgraphs.Clear();
            FieldSubgraphs.Clear();

            foreach (var subgraph in context.Subgraphs)
            {
                Subgraphs.Add(subgraph.Name);
            }
        }
    }
}
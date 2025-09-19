using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning.Partitioners;

/// <summary>
/// Partitions a selection set by grouping selections according to their target types.
/// Creates a shared selection set containing fields that apply to the base type, and separate
/// selection sets for each concrete type that include both shared selections and type-specific selections.
/// This enables efficient query planning by allowing the planner to generate optimized requests
/// for each concrete type while preserving fragment directives and type conditions.
/// </summary>
internal sealed class SelectionSetByTypePartitioner(FusionSchemaDefinition schema)
{
    public SelectionSetByTypePartitionerResult Partition(SelectionSetByTypePartitionerInput input)
    {
        var indexBuilder = input.SelectionSetIndex.ToBuilder();
        var context = new Context { SharedType = input.SelectionSet.Type, SelectionSetIndexBuilder = indexBuilder };

        CollectSelections(input.SelectionSet.Node, input.SelectionSet.Type, context);

        var sharedSelectionSet =
            context.SharedSelections?.Count > 0
                ? new SelectionSetNode(context.SharedSelections)
                : null;

        if (sharedSelectionSet is not null)
        {
            indexBuilder.Register(input.SelectionSet.Id, sharedSelectionSet);
        }

        var selectionSetByType = ImmutableArray.CreateBuilder<SelectionSetByType>(context.SelectionsByType.Count);
        foreach (var (type, selections) in context.SelectionsByType.OrderBy(x => x.Key))
        {
            var selectionSetNode = new SelectionSetNode(
            [
                ..context.SharedSelections ?? [],
                ..selections
            ]);

            indexBuilder.Register(input.SelectionSet.Id, selectionSetNode);

            selectionSetByType.Add(new SelectionSetByType((FusionObjectTypeDefinition)schema.Types[type], selectionSetNode));
        }

        return new SelectionSetByTypePartitionerResult(sharedSelectionSet, selectionSetByType.ToImmutable(), indexBuilder);
    }

    private void CollectSelections(
        SelectionSetNode selectionSet,
        ITypeDefinition type,
        Context context)
    {
        List<ISelectionNode>? selections = null;

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                    selections ??= [];
                    selections.Add(fieldNode);
                    break;

                case InlineFragmentNode inlineFragmentNode:
                    var typeCondition = type;

                    if (inlineFragmentNode.TypeCondition is { Name.Value: { } name })
                    {
                        typeCondition = schema.Types[name].AsTypeDefinition();
                    }

                    var hasDirectives = inlineFragmentNode.Directives.Any();

                    if (hasDirectives)
                    {
                        context.FragmentPath.Push(inlineFragmentNode.WithTypeCondition(null));
                    }

                    context.TypePath.Push(typeCondition);

                    CollectSelections(inlineFragmentNode.SelectionSet, typeCondition, context);

                    context.TypePath.Pop();

                    if (hasDirectives)
                    {
                        context.FragmentPath.Pop();
                    }

                    break;
            }
        }

        if (selections is null)
        {
            return;
        }

        var selectionsWithPath = GetSelectionsWithPath(context.FragmentPath, selections);

        if (type == context.SharedType)
        {
            context.SharedSelections ??= [];
            context.SharedSelections.AddRange(selectionsWithPath);
        }
        else if (type.IsInterfaceType())
        {
            if (context.TryGetParentConcreteType(out var objectType))
            {
                AddSelectionsForConcreteType(context, objectType, selectionsWithPath);
            }
            else
            {
                foreach (var possibleType in schema.GetPossibleTypes(type))
                {
                    AddSelectionsForConcreteType(context, possibleType, selectionsWithPath, cloneSelectionSets: true);
                }
            }
        }
        else if (type is FusionObjectTypeDefinition objectType)
        {
            AddSelectionsForConcreteType(context, objectType, selectionsWithPath);
        }
    }

    private void AddSelectionsForConcreteType(
        Context context,
        FusionObjectTypeDefinition type,
        List<ISelectionNode> selections,
        bool cloneSelectionSets = false)
    {
        if (!context.SelectionsByType.TryGetValue(type.Name, out var typeSelections))
        {
            typeSelections = [];
            context.SelectionsByType.Add(type.Name, typeSelections);
        }

        if (cloneSelectionSets)
        {
            var rewrittenSelections = new List<ISelectionNode>(selections.Count);

            foreach (var selection in selections)
            {
                var rewrittenSelection = SyntaxRewriter.Create(
                    node =>
                    {
                        if (node is SelectionSetNode selectionSetNode)
                        {
                            var newSelectionSet = new SelectionSetNode(selectionSetNode.Selections);

                            // Since we're cloning the selection set,
                            // we also need to keep track of the original
                            // selection set the cloned one belongs to,
                            // so we can later insert requirements in the original one.
                            context.SelectionSetIndexBuilder.RegisterCloned(
                                selectionSetNode,
                                newSelectionSet);

                            return newSelectionSet;
                        }

                        return node;
                    }).Rewrite(selection)!;

                rewrittenSelections.Add(rewrittenSelection);
            }

            typeSelections.AddRange(rewrittenSelections);
        }
        else
        {
            typeSelections.AddRange(selections);
        }
    }

    private static List<ISelectionNode> GetSelectionsWithPath(
        Stack<InlineFragmentNode> fragmentPath,
        List<ISelectionNode> selections)
    {
        var start = selections;

        foreach (var fragment in fragmentPath)
        {
            start = [fragment.WithSelectionSet(new SelectionSetNode(start))];
        }

        return start;
    }

    private class Context
    {
        /// <summary>
        /// Gets the selections by type.
        /// The key is the type name and the value the selections for that type.
        /// </summary>
        public Dictionary<string, List<ISelectionNode>> SelectionsByType { get; } = [];

        /// <summary>
        /// Gets the fragment path.
        /// This is pushed to whenever we enter an inline fragment with directives,
        /// in order to preserve those.
        /// </summary>
        public Stack<InlineFragmentNode> FragmentPath { get; } = new();

        /// <summary>
        /// Gets the type path.
        /// This is pushed to whenever we enter an inline fragment with a type condition.
        /// </summary>
        public Stack<ITypeDefinition> TypePath { get; } = new();

        /// <summary>
        /// Gets the shared type.
        /// </summary>
        public required ITypeDefinition SharedType { get; init; }

        /// <summary>
        /// Gets the selections for the <see cref="SharedType" />.
        /// </summary>
        public List<ISelectionNode>? SharedSelections { get; set; }

        public required SelectionSetIndexBuilder SelectionSetIndexBuilder { get; init; }

        public bool TryGetParentConcreteType([NotNullWhen(true)] out FusionObjectTypeDefinition? objectType)
        {
            foreach (var type in TypePath)
            {
                if (type is FusionObjectTypeDefinition obj)
                {
                    objectType = obj;
                    return true;
                }
            }

            objectType = null;
            return false;
        }
    }
}

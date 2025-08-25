using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning.Partitioners;

/// <summary>
/// Partitions a root selection set by separating `node` fields from regular fields.
/// This enables the operation planner to handle node-based queries separately from
/// the standard field resolution, which is essential for proper operation planning.
/// Node fields are collected with their associated fragment directives preserved
/// for correct execution context.
/// </summary>
internal sealed class NodeFieldSelectionSetPartitioner(FusionSchemaDefinition schema)
{
    public RootSelectionSetPartitionerResult Partition(RootSelectionSetPartitionerInput input)
    {
        var context = new Context();

        var selectionSet = RewriteSelectionSet(input.SelectionSet.Node, context);

        if (context.NodeFields is null)
        {
            return new RootSelectionSetPartitionerResult(input.SelectionSet, null, input.SelectionSetIndex);
        }

        var indexBuilder = input.SelectionSetIndex.ToBuilder();

        SelectionSet? prunedSelectionSet = null;
        if (selectionSet is not null)
        {
            indexBuilder.Register(input.SelectionSet.Node, selectionSet);

            prunedSelectionSet = input.SelectionSet with
            {
                Id = indexBuilder.GetId(selectionSet),
                Node = selectionSet
            };
        }

        return new RootSelectionSetPartitionerResult(prunedSelectionSet, context.NodeFields, indexBuilder);
    }

    private SelectionSetNode? RewriteSelectionSet(SelectionSetNode selectionSet, Context context)
    {
        List<ISelectionNode>? selections = null;

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode when schema.QueryType.Fields.TryGetField(fieldNode.Name.Value, out var field)
                    && field is { Name: "node", Type: IInterfaceTypeDefinition { Name: "Node" } }:
                    var directives = new List<DirectiveNode>(fieldNode.Directives);
                    foreach (var fragment in context.FragmentPath)
                    {
                        directives.AddRange(fragment.Directives);
                    }

                    context.NodeFields ??= [];
                    context.NodeFields.Add(fieldNode.WithDirectives(directives));
                    break;

                case FieldNode:
                    selections ??= [];
                    selections.Add(selection);
                    break;

                case InlineFragmentNode inlineFragmentNode:
                    var hasDirectives = inlineFragmentNode.Directives.Any();

                    if (hasDirectives)
                    {
                        context.FragmentPath.Push(inlineFragmentNode);
                    }

                    var fragmentSelectionSet = RewriteSelectionSet(
                        inlineFragmentNode.SelectionSet,
                        context);

                    if (hasDirectives)
                    {
                        context.FragmentPath.Pop();
                    }

                    if (fragmentSelectionSet is not null)
                    {
                        selections ??= [];
                        selections.Add(inlineFragmentNode.WithSelectionSet(fragmentSelectionSet));
                    }

                    break;
            }
        }

        if (selections is null || selections.Count < 1)
        {
            return null;
        }

        return new SelectionSetNode(selections);
    }

    private class Context
    {
        /// <summary>
        /// Gets the fragment path.
        /// This is pushed to whenever we enter an inline fragment with directives,
        /// in order to preserve those.
        /// </summary>
        public List<InlineFragmentNode> FragmentPath { get; } = [];

        public List<FieldNode>? NodeFields { get; set; }
    }
}

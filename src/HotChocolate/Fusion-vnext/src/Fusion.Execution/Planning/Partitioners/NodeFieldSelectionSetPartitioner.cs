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
            indexBuilder.Register(selectionSet);

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
                case FieldNode fieldNode:
                    var field = schema.QueryType.Fields[fieldNode.Name.Value];

                    if (field.IsIntrospectionField)
                    {
                        continue;
                    }

                    if (field is { Name: "node", Type: IInterfaceTypeDefinition { Name: "Node" } })
                    {
                        var nodeField = new NodeField { Field = fieldNode, ParentFragments = context.FragmentPath?.ToArray() };

                        context.NodeFields ??= [];
                        context.NodeFields.Add(nodeField);
                    }
                    else
                    {
                        selections ??= [];
                        selections.Add(selection);
                    }

                    break;

                case InlineFragmentNode inlineFragmentNode:
                    context.FragmentPath ??= [];
                    context.FragmentPath.Push(inlineFragmentNode);

                    var fragmentSelectionSet = RewriteSelectionSet(
                        inlineFragmentNode.SelectionSet,
                        context);

                    context.FragmentPath?.Pop();

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
        public List<NodeField>? NodeFields { get; set; }

        public Stack<InlineFragmentNode>? FragmentPath { get; set; }
    }
}

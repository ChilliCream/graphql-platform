using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Utilities;

internal static class FieldSelectionMapUtilities
{
    public static FieldPath CreateFieldPath(ImmutableStack<SelectionPlanNode> path)
    {
        var current = FieldPath.Root;

        foreach (var segment in path.Reverse())
        {
            if (segment is FieldPlanNode field)
            {
                current = current.Append(field.Field.Name);
            }
        }

        return current;
    }

    public static FieldNode ToFieldNode(this FieldPath path)
    {
        var current = new FieldNode(path.Name);

        foreach (var segment in path.Skip(1))
        {
            current = new FieldNode(
                null,
                new NameNode(segment.Name),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode([current]));
        }

        return current;
    }

    public static SelectionSetNode ToSelectionSetNode(this ImmutableArray<FieldPath> paths)
    {
        var selections = new ISelectionNode[paths.Length];

        for (var i = 0; i < paths.Length; i++)
        {
            selections[i] = paths[i].ToFieldNode();
        }

        return new SelectionSetNode(selections);
    }
}

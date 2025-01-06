using System.Collections.Immutable;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Utilities;

internal static class FieldSelectionMapUtilities
{
    public static bool IsMergeableWith(this FieldPlanNode field, FieldNode y)
        => field.FieldNode.IsMergeableWith(y);

    public static bool IsMergeableWith(this FieldNode x, FieldNode y)
        => FieldNodeMergeComparer.Instance.Equals(x, y);

    public static SelectionPath CreateSelectionPath(this ImmutableStack<SelectionPlanNode> path)
    {
        var current = SelectionPath.Root;

        foreach (var segment in path.Reverse())
        {
            if (segment is FieldPlanNode field)
            {
                current = current.AppendField(field.Field.Name);
            }
            else if(segment is InlineFragmentPlanNode fragment)
            {
                current = current.AppendFragment(fragment.DeclaringType.Name);
            }
        }

        return current;
    }

    public static ISelectionNode ToSelectionNode(this SelectionPath path)
    {
        var current = CreateFrom(path);

        if(path.Segments.Length == 1)
        {
            return current;
        }

        for (var i = path.Segments.Length - 1; i >= 1; i--)
        {
            current = CreateFrom(path.Segments[i], current);
        }

        return current;

        static ISelectionNode CreateFrom(SelectionPath segment, ISelectionNode? previous = null)
        {
            var selectionSet =
                previous is not null
                    ? new SelectionSetNode([previous])
                    : null;

            if(segment.Kind == SelectionPathSegmentKind.InlineFragment)
            {
                if (selectionSet is null)
                {
                    throw new InvalidOperationException(
                        $"The provided path `${segment}` is invalid.");
                }

                return new InlineFragmentNode(
                    null,
                    new NamedTypeNode(new NameNode(segment.Name)),
                    Array.Empty<DirectiveNode>(),
                    selectionSet);
            }

            return new FieldNode(
                null,
                new NameNode(segment.Name),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                selectionSet);
        }
    }

    public static SelectionSetNode ToSelectionSetNode(this ImmutableArray<SelectionPath>.Builder paths)
    {
        var selections = new ISelectionNode[paths.Count];

        for (var i = 0; i < paths.Count; i++)
        {
            selections[i] = paths[i].ToSelectionNode();
        }

        return new SelectionSetNode(selections);
    }

    public static SelectionSetNode ToSelectionSetNode(this ImmutableArray<SelectionPath> paths)
    {
        var selections = new ISelectionNode[paths.Length];

        for (var i = 0; i < paths.Length; i++)
        {
            selections[i] = paths[i].ToSelectionNode();
        }

        return new SelectionSetNode(selections);
    }
}

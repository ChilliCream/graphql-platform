using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Utilities;

internal static class FieldSelectionMapUtilities
{
    public static FieldNode ToFieldNode(this FieldPath path)
    {
        var current = new FieldNode(path.Name);

        foreach (var segment in path.Skip(1))
        {
            current = new FieldNode(
                null,
                new NameNode(segment.Name),
                null,
                [],
                [],
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

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

#nullable disable
    public static SelectionSetNode ToSelectionSetNode(this ImmutableArray<FieldPath> paths)
    {
        var selections = new List<ISelectionNode>();

        foreach (var path in paths)
        {
            if (path is null)
            {
                continue;
            }

            selections.Add(path.ToFieldNode());
        }

        return new SelectionSetNode(selections);
    }
#nullable restore
}

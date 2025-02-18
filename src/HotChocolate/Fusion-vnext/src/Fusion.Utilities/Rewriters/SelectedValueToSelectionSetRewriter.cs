using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using GraphQLNameNode = HotChocolate.Language.NameNode;

namespace HotChocolate.Fusion.Rewriters;

public sealed class SelectedValueToSelectionSetRewriter
{
    public static SelectionSetNode SelectedValueToSelectionSet(SelectedValueNode selectedValue)
    {
        return new SelectionSetNode(Visit(selectedValue));
    }

    private static List<ISelectionNode> Visit(SelectedValueNode selectedValue)
    {
        var selections = Visit(selectedValue.SelectedValueEntry);

        if (selectedValue.SelectedValue is not null)
        {
            // FIXME: Merge with selections above. Waiting for selection set merge utility.
            selections.AddRange(Visit(selectedValue.SelectedValue));
        }

        return selections;
    }

    private static List<ISelectionNode> Visit(SelectedValueEntryNode selectedValueEntry)
    {
        var selections = new List<ISelectionNode>();

        List<ISelectionNode>? selectedObjectValueSelections = null;

        if (selectedValueEntry.SelectedObjectValue is not null)
        {
            selectedObjectValueSelections = Visit(selectedValueEntry.SelectedObjectValue);

            if (selectedValueEntry.Path is null)
            {
                selections.AddRange(selectedObjectValueSelections);
            }
        }

        if (selectedValueEntry.Path is not null)
        {
            List<ISelectionNode>? selectedListValueSelections = null;

            if (selectedValueEntry.SelectedListValue is not null)
            {
                selectedListValueSelections = Visit(selectedValueEntry.SelectedListValue);
            }

            var pathSelections = Visit(
                selectedValueEntry.Path,
                selectedObjectValueSelections,
                selectedListValueSelections);

            selections.AddRange(pathSelections);
        }

        return selections;
    }

    private static List<ISelectionNode> Visit(PathNode path,
        List<ISelectionNode>? selectedObjectValueSelections,
        List<ISelectionNode>? selectedListValueSelections)
    {
        var selections = new List<ISelectionNode>();
        var pathSegmentSelections = Visit(
            path.PathSegment,
            selectedObjectValueSelections,
            selectedListValueSelections);

        if (path.TypeName is not null)
        {
            selections.Add(
                new InlineFragmentNode(
                    null,
                    new NamedTypeNode(path.TypeName.Value),
                    [],
                    new SelectionSetNode(pathSegmentSelections)));
        }
        else
        {
            selections.AddRange(pathSegmentSelections);
        }

        return selections;
    }

    private static List<ISelectionNode> Visit(
        PathSegmentNode pathSegment,
        List<ISelectionNode>? selectedObjectValueSelections,
        List<ISelectionNode>? selectedListValueSelections)
    {
        var selections = new List<ISelectionNode>();

        List<ISelectionNode>? subSelections = null;

        // This is the last segment of the path.
        if (pathSegment.PathSegment is null)
        {
            // "a.b.{ c d }" ("c" and "d" are the sub-selections added to the path segment "b").
            if (selectedObjectValueSelections is not null)
            {
                subSelections = selectedObjectValueSelections;
            }
            // "a.b[{ c d }]" ("c" and "d" are the sub-selections added to the path segment "b").
            else if (selectedListValueSelections is not null)
            {
                subSelections = selectedListValueSelections;
            }
        }
        else
        {
            subSelections = Visit(
                pathSegment.PathSegment,
                selectedObjectValueSelections,
                selectedListValueSelections);
        }

        var subSelectionSet = subSelections is null || subSelections.Count == 0
            ? null
            : new SelectionSetNode(subSelections);

        if (subSelectionSet is not null && pathSegment.TypeName is { } typeName)
        {
            subSelectionSet = new SelectionSetNode([
                new InlineFragmentNode(
                    null,
                    new NamedTypeNode(typeName.Value),
                    [],
                    subSelectionSet)]);
        }

        selections.Add(
            new FieldNode(
                new GraphQLNameNode(pathSegment.FieldName.Value),
                null,
                [],
                [],
                subSelectionSet));

        return selections;
    }

    private static List<ISelectionNode> Visit(SelectedObjectValueNode selectedObjectValue)
    {
        var selections = new List<ISelectionNode>();

        foreach (var field in selectedObjectValue.Fields)
        {
            if (field.SelectedValue is null)
            {
                selections.Add(new FieldNode(field.Name.Value));
            }
            else
            {
                // FIXME: Merge selections. Waiting for selection set merge utility.
                selections.AddRange(Visit(field.SelectedValue));
            }
        }

        return selections;
    }

    private static List<ISelectionNode> Visit(SelectedListValueNode selectedListValue)
    {
        var selections = new List<ISelectionNode>();

        if (selectedListValue.SelectedValue is not null)
        {
            selections.AddRange(Visit(selectedListValue.SelectedValue));
        }

        if (selectedListValue.SelectedListValue is not null)
        {
            selections.AddRange(Visit(selectedListValue.SelectedListValue));
        }

        return selections;
    }
}

using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;
using GraphQLNameNode = HotChocolate.Language.NameNode;

namespace HotChocolate.Fusion.Rewriters;

public sealed class SelectedValueToSelectionSetRewriter(ISchemaDefinition schema)
{
    private readonly MergeSelectionSetRewriter _mergeSelectionSetRewriter = new(schema);

    public SelectionSetNode SelectedValueToSelectionSet(
        ChoiceValueSelectionNode choiceValueSelection,
        ITypeDefinition type)
    {
        var selections = Visit(choiceValueSelection);
        var selectionSets = selections.Select(s => new SelectionSetNode([s])).ToArray();

        return _mergeSelectionSetRewriter.Merge(selectionSets, type);
    }

    private static List<ISelectionNode> Visit(ChoiceValueSelectionNode choiceValueSelection)
    {
        var selections = Visit(choiceValueSelection.Entries);

        if (choiceValueSelection.SelectedValue is not null)
        {
            selections.AddRange(Visit(choiceValueSelection.SelectedValue));
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

    private static List<ISelectionNode> Visit(ObjectValueSelectionNode objectValueSelection)
    {
        var selections = new List<ISelectionNode>();

        foreach (var field in objectValueSelection.Fields)
        {
            if (field.ValueSelection is null)
            {
                selections.Add(new FieldNode(field.Name.Value));
            }
            else
            {
                selections.AddRange(Visit(field.ValueSelection));
            }
        }

        return selections;
    }

    private static List<ISelectionNode> Visit(ListValueSelectionNode listValueSelection)
    {
        var selections = new List<ISelectionNode>();

        if (listValueSelection.SelectedValue is not null)
        {
            selections.AddRange(Visit(listValueSelection.SelectedValue));
        }

        if (listValueSelection.ListValueSelection is not null)
        {
            selections.AddRange(Visit(listValueSelection.ListValueSelection));
        }

        return selections;
    }
}

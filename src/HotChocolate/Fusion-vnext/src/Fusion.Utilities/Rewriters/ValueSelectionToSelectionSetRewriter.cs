using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class ValueSelectionToSelectionSetRewriter(ISchemaDefinition schema)
{
    private readonly MergeSelectionSetRewriter _mergeSelectionSetRewriter = new(schema);

    public SelectionSetNode Rewrite(IValueSelectionNode node, ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(type);

        return _mergeSelectionSetRewriter.Merge([new SelectionSetNode([Visit(node)])], type);
    }

    public SelectionSetNode Rewrite(IEnumerable<IValueSelectionNode> nodes, ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var selections = new List<ISelectionNode>();

        foreach (var node in nodes)
        {
            selections.Add(Visit(node));
        }

        if (selections.Count == 0)
        {
            throw new ArgumentException(
                "At least one value selection is required.");
        }

        return _mergeSelectionSetRewriter.Merge([new SelectionSetNode(selections)], type);
    }

    private static ISelectionNode Visit(IValueSelectionNode node)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                return Visit(choice);

            case PathNode pathNode:
                return Visit(pathNode, null);

            case ListValueSelectionNode list:
                return Visit(list);

            case PathListValueSelectionNode list:
                return Visit(list);

            case ObjectValueSelectionNode obj:
                return Visit(obj);

            case PathObjectValueSelectionNode obj:
                return Visit(obj);

            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
    }

    private static ISelectionNode Visit(ChoiceValueSelectionNode node)
    {
        var selections = new List<ISelectionNode>();

        foreach (var brand in node.Branches)
        {
            selections.Add(Visit(brand));
        }

        return new InlineFragmentNode(null, null, [], new SelectionSetNode(selections));
    }

    private static ISelectionNode Visit(PathNode path, ISelectionNode? selection)
    {
        var pathSelection = Visit(path.PathSegment, selection);

        if (path.TypeName is not null)
        {
            return new InlineFragmentNode(
                null,
                new NamedTypeNode(path.TypeName.Value),
                [],
                new SelectionSetNode([pathSelection]));
        }

        return pathSelection;
    }

    private static FieldNode Visit(PathSegmentNode pathSegment, ISelectionNode? selection)
    {
        var selectionSet =
            CreateSelectionSetNode(
                pathSegment.PathSegment is null
                    ? selection
                    : Visit(pathSegment.PathSegment, selection));

        if (selectionSet is not null && pathSegment.TypeName is { } typeName)
        {
            selectionSet = new SelectionSetNode([
                new InlineFragmentNode(
                    null,
                    new NamedTypeNode(typeName.Value),
                    [],
                    selectionSet)
            ]);
        }

        return new FieldNode(pathSegment.FieldName.Value, selectionSet);

        static SelectionSetNode? CreateSelectionSetNode(ISelectionNode? selection)
            => selection is null ? null : new SelectionSetNode([selection]);
    }

    private static ISelectionNode Visit(PathObjectValueSelectionNode node)
    {
        var objectSelection = Visit(node.ObjectValueSelection);
        return Visit(node.Path, objectSelection);
    }

    private static ISelectionNode Visit(ObjectValueSelectionNode node)
    {
        var selections = new List<ISelectionNode>();

        foreach (var field in node.Fields)
        {
            if (field.ValueSelection is null)
            {
                selections.Add(new FieldNode(field.Name.Value));
            }
            else
            {
                selections.Add(Visit(field.ValueSelection));
            }
        }

        return new InlineFragmentNode(null, null, [], new SelectionSetNode(selections));
    }

    private static ISelectionNode Visit(PathListValueSelectionNode node)
    {
        var listSelection = Visit(node.ListValueSelection);
        return Visit(node.Path, listSelection);
    }

    private static ISelectionNode Visit(ListValueSelectionNode node)
        => Visit(node.ElementSelection);
}

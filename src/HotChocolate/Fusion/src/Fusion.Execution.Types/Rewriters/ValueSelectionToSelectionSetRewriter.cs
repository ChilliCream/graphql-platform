using System.Collections.Immutable;
using HotChocolate.Fusion.Converters;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Rewriters;

public sealed class ValueSelectionToSelectionSetRewriter(
    FusionSchemaDefinition schema)
{
    private readonly MergeSelectionSetRewriter _mergeSelectionSetRewriter
        = new(schema);

    /// <summary>
    /// Turns a value selection into a selection set rooted at the given type. Context paths
    /// add no selections.
    /// </summary>
    /// <param name="node">The value selection to convert.</param>
    /// <param name="type">The type the selection set is rooted at.</param>
    /// <returns>
    /// The selection set, or one without selections if the value selection only reads from
    /// contexts.
    /// </returns>
    public SelectionSetNode Rewrite(IValueSelectionNode node, ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(type);

        if (Visit(node) is not { } selection)
        {
            return new SelectionSetNode([]);
        }

        return _mergeSelectionSetRewriter.Merge([new SelectionSetNode([selection])], type);
    }

    /// <summary>
    /// Turns the value selections into a single selection set rooted at the given type.
    /// Context paths add no selections.
    /// </summary>
    /// <param name="nodes">The value selections to convert.</param>
    /// <param name="type">The type the selection set is rooted at.</param>
    /// <returns>
    /// The selection set, or one without selections if the value selections only read from
    /// contexts.
    /// </returns>
    public SelectionSetNode Rewrite(IEnumerable<IValueSelectionNode> nodes, ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var selections = new List<ISelectionNode>();
        var count = 0;

        foreach (var node in nodes)
        {
            count++;

            if (Visit(node) is { } selection)
            {
                selections.Add(selection);
            }
        }

        if (count == 0)
        {
            throw new ArgumentException(
                "At least one value selection is required.");
        }

        if (selections.Count == 0)
        {
            return new SelectionSetNode([]);
        }

        return _mergeSelectionSetRewriter.Merge([new SelectionSetNode(selections)], type);
    }

    /// <summary>
    /// Turns the value selections into a single selection set without using a schema, so it can
    /// run before the schema is built. Unlike the schema-based overloads, it does not merge or
    /// remove duplicate selections. Context paths add no selections.
    /// </summary>
    /// <param name="nodes">
    /// The value selections to convert.
    /// </param>
    /// <returns>
    /// A selection set that represents the given value selections.
    /// </returns>
    public static SelectionSetNode Rewrite(IEnumerable<IValueSelectionNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var selections = new List<ISelectionNode>();

        foreach (var node in nodes)
        {
            if (Visit(node) is { } selection)
            {
                Flatten(selection, selections);
            }
        }

        return new SelectionSetNode(selections);
    }

    /// <summary>
    /// Collects the selections that the context paths of the value selections read, grouped by
    /// context name. Each selection set is rooted at the value that the context sets.
    /// </summary>
    /// <param name="nodes">
    /// The value selections to convert.
    /// </param>
    /// <returns>
    /// One selection set per context name, empty if the value selections have no context path.
    /// </returns>
    public static ImmutableDictionary<string, SelectionSetNode> RewriteContextPaths(
        IEnumerable<IValueSelectionNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        Dictionary<string, List<ISelectionNode>>? contexts = null;

        foreach (var node in nodes)
        {
            CollectContextPaths(node, ref contexts);
        }

        if (contexts is null)
        {
#if NET10_0_OR_GREATER
            return [];
#else
            return ImmutableDictionary<string, SelectionSetNode>.Empty;
#endif
        }

        var builder = ImmutableDictionary.CreateBuilder<string, SelectionSetNode>(StringComparer.Ordinal);

        foreach (var (name, selections) in contexts)
        {
            builder.Add(name, new SelectionSetNode(selections));
        }

        return builder.ToImmutable();
    }

    private static void CollectContextPaths(
        IValueSelectionNode node,
        ref Dictionary<string, List<ISelectionNode>>? contexts)
    {
        switch (node)
        {
            case PathNode { ContextName: { } contextName } path:
                AddContextSelection(ref contexts, contextName.Value, VisitPath(path, null));
                break;

            case ChoiceValueSelectionNode choice:
                foreach (var branch in choice.Branches)
                {
                    CollectContextPaths(branch, ref contexts);
                }

                break;

            case ListValueSelectionNode list:
                CollectContextPaths(list.ElementSelection, ref contexts);
                break;

            case PathListValueSelectionNode pathList:
                if (pathList.Path.ContextName is { } listContextName
                    && Visit(pathList.ListValueSelection) is { } listInner)
                {
                    AddContextSelection(
                        ref contexts,
                        listContextName.Value,
                        VisitPath(pathList.Path, listInner));
                }

                CollectContextPaths(pathList.ListValueSelection, ref contexts);
                break;

            case ObjectValueSelectionNode obj:
                CollectContextPaths(obj, ref contexts);
                break;

            case PathObjectValueSelectionNode pathObj:
                if (pathObj.Path.ContextName is { } objContextName
                    && Visit(pathObj.ObjectValueSelection) is { } objInner)
                {
                    AddContextSelection(
                        ref contexts,
                        objContextName.Value,
                        VisitPath(pathObj.Path, objInner));
                }

                CollectContextPaths(pathObj.ObjectValueSelection, ref contexts);
                break;
        }
    }

    private static void AddContextSelection(
        ref Dictionary<string, List<ISelectionNode>>? contexts,
        string contextName,
        ISelectionNode selection)
    {
        contexts ??= new Dictionary<string, List<ISelectionNode>>(StringComparer.Ordinal);

        if (!contexts.TryGetValue(contextName, out var selections))
        {
            selections = [];
            contexts.Add(contextName, selections);
        }

        selections.Add(selection);
    }

    private static void CollectContextPaths(
        ObjectValueSelectionNode node,
        ref Dictionary<string, List<ISelectionNode>>? contexts)
    {
        foreach (var field in node.Fields)
        {
            if (field.ValueSelection is not null)
            {
                CollectContextPaths(field.ValueSelection, ref contexts);
            }
        }
    }

    private static void Flatten(ISelectionNode selection, List<ISelectionNode> selections)
    {
        // An inline fragment without a type condition only groups selections, so we inline its
        // members to keep the resulting selection list flat. Type-conditioned inline fragments and
        // fields are preserved as-is.
        if (selection is InlineFragmentNode { TypeCondition: null } inlineFragment)
        {
            foreach (var inner in inlineFragment.SelectionSet.Selections)
            {
                Flatten(inner, selections);
            }

            return;
        }

        selections.Add(selection);
    }

    private static ISelectionNode? Visit(IValueSelectionNode node)
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

    private static ISelectionNode? Visit(ChoiceValueSelectionNode node)
    {
        var selections = new List<ISelectionNode>();

        foreach (var brand in node.Branches)
        {
            if (Visit(brand) is { } selection)
            {
                selections.Add(selection);
            }
        }

        if (selections.Count == 0)
        {
            return null;
        }

        return new InlineFragmentNode(null, null, [], new SelectionSetNode(selections));
    }

    private static ISelectionNode? Visit(PathNode path, ISelectionNode? selection)
        => path.ContextName is null ? VisitPath(path, selection) : null;

    private static ISelectionNode VisitPath(PathNode path, ISelectionNode? selection)
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

        return new FieldNode(
            new HotChocolate.Language.NameNode(pathSegment.FieldName.Value),
            null,
            [],
            FieldSelectionMapValueNodeConverter.Convert(pathSegment.Arguments),
            selectionSet);

        static SelectionSetNode? CreateSelectionSetNode(ISelectionNode? selection)
            => selection is null ? null : new SelectionSetNode([selection]);
    }

    private static ISelectionNode? Visit(PathObjectValueSelectionNode node)
    {
        if (Visit(node.ObjectValueSelection) is not { } objectSelection)
        {
            return null;
        }

        return Visit(node.Path, objectSelection);
    }

    private static ISelectionNode? Visit(ObjectValueSelectionNode node)
    {
        var selections = new List<ISelectionNode>();

        foreach (var field in node.Fields)
        {
            if (field.ValueSelection is null)
            {
                selections.Add(
                    new FieldNode(
                        new HotChocolate.Language.NameNode(field.Name.Value),
                        null,
                        [],
                        FieldSelectionMapValueNodeConverter.Convert(field.Arguments),
                        null));
            }
            else if (Visit(field.ValueSelection) is { } selection)
            {
                selections.Add(selection);
            }
        }

        if (selections.Count == 0)
        {
            return null;
        }

        return new InlineFragmentNode(null, null, [], new SelectionSetNode(selections));
    }

    private static ISelectionNode? Visit(PathListValueSelectionNode node)
    {
        if (Visit(node.ListValueSelection) is not { } listSelection)
        {
            return null;
        }

        return Visit(node.Path, listSelection);
    }

    private static ISelectionNode? Visit(ListValueSelectionNode node)
        => Visit(node.ElementSelection);
}

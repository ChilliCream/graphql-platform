using System.Collections.Immutable;
using HotChocolate.Fusion.Converters;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class ValueSelectionToSelectionSetRewriter(
    ISchemaDefinition schema,
    bool ignoreMissingTypeSystemMembers = false)
{
    private readonly MergeSelectionSetRewriter _mergeSelectionSetRewriter
        = new(schema, ignoreMissingTypeSystemMembers);

    /// <summary>
    /// Turns the value selection into a selection set on the given type. Context paths add no
    /// selections.
    /// </summary>
    /// <param name="node">
    /// The value selection to convert.
    /// </param>
    /// <param name="type">
    /// The type the selection set is declared on.
    /// </param>
    /// <returns>
    /// The selection set, or a selection set without selections if the value selection only
    /// consists of context paths.
    /// </returns>
    public SelectionSetNode Rewrite(IValueSelectionNode node, ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(type);

        var selection = Visit(node);

        if (selection is null)
        {
            return new SelectionSetNode([]);
        }

        return _mergeSelectionSetRewriter.Merge([new SelectionSetNode([selection])], type);
    }

    /// <summary>
    /// Turns the value selections into a selection set on the given type. Context paths add no
    /// selections.
    /// </summary>
    /// <param name="nodes">
    /// The value selections to convert.
    /// </param>
    /// <param name="type">
    /// The type the selection set is declared on.
    /// </param>
    /// <returns>
    /// The selection set, or a selection set without selections if the value selections only
    /// consist of context paths.
    /// </returns>
    public SelectionSetNode Rewrite(IEnumerable<IValueSelectionNode> nodes, ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var selections = new List<ISelectionNode>();
        var hasNodes = false;

        foreach (var node in nodes)
        {
            hasNodes = true;

            if (Visit(node) is { } selection)
            {
                selections.Add(selection);
            }
        }

        if (!hasNodes)
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
    /// Turns the context paths of the value selections into one selection set per context name
    /// without using a schema. The selection set of a context describes the value that sets the
    /// context, so <c>$name.a.b</c> results in <c>a { b }</c>. Paths that do not start at a
    /// context are ignored, and like the schema-free <c>Rewrite</c> it does not merge or remove
    /// duplicate selections.
    /// </summary>
    /// <param name="nodes">
    /// The value selections to scan for context paths.
    /// </param>
    /// <returns>
    /// The selection sets keyed by context name.
    /// </returns>
    public static ImmutableDictionary<string, SelectionSetNode> RewriteContextPaths(
        IEnumerable<IValueSelectionNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var contexts = new Dictionary<string, List<ISelectionNode>>();

        foreach (var node in nodes)
        {
            CollectContextPaths(node, contexts);
        }

        var result = ImmutableDictionary.CreateBuilder<string, SelectionSetNode>();

        foreach (var (name, selections) in contexts)
        {
            result.Add(name, new SelectionSetNode(selections));
        }

        return result.ToImmutable();
    }

    private static void CollectContextPaths(
        IValueSelectionNode node,
        Dictionary<string, List<ISelectionNode>> contexts)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                foreach (var branch in choice.Branches)
                {
                    CollectContextPaths(branch, contexts);
                }

                break;

            case PathNode { ContextName: { } contextName } path:
                AddContextSelection(contexts, contextName, CreatePathSelection(path, null));
                break;

            case PathObjectValueSelectionNode { Path.ContextName: { } contextName } obj:
                AddContextSelection(
                    contexts,
                    contextName,
                    CreatePathSelection(obj.Path, Visit(obj.ObjectValueSelection)));
                break;

            case PathObjectValueSelectionNode obj:
                CollectContextPaths(obj.ObjectValueSelection, contexts);
                break;

            case PathListValueSelectionNode { Path.ContextName: { } contextName } list:
                AddContextSelection(
                    contexts,
                    contextName,
                    CreatePathSelection(list.Path, Visit(list.ListValueSelection)));
                break;

            case PathListValueSelectionNode list:
                CollectContextPaths(list.ListValueSelection, contexts);
                break;

            case ListValueSelectionNode list:
                CollectContextPaths(list.ElementSelection, contexts);
                break;

            case ObjectValueSelectionNode obj:
                foreach (var field in obj.Fields)
                {
                    if (field.ValueSelection is not null)
                    {
                        CollectContextPaths(field.ValueSelection, contexts);
                    }
                }

                break;
        }
    }

    private static void AddContextSelection(
        Dictionary<string, List<ISelectionNode>> contexts,
        HotChocolate.Fusion.Language.NameNode contextName,
        ISelectionNode selection)
    {
        if (!contexts.TryGetValue(contextName.Value, out var selections))
        {
            selections = [];
            contexts.Add(contextName.Value, selections);
        }

        Flatten(selection, selections);
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

    // Returns null if the node only consists of context paths and therefore selects nothing.
    private static ISelectionNode? Visit(IValueSelectionNode node)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                return Visit(choice);

            case PathNode pathNode:
                return Visit(pathNode);

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

        foreach (var branch in node.Branches)
        {
            if (Visit(branch) is { } selection)
            {
                selections.Add(selection);
            }
        }

        return selections.Count == 0
            ? null
            : new InlineFragmentNode(null, null, [], new SelectionSetNode(selections));
    }

    private static ISelectionNode? Visit(PathNode path)
        => path.ContextName is null ? CreatePathSelection(path, null) : null;

    private static ISelectionNode CreatePathSelection(PathNode path, ISelectionNode? selection)
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
        if (node.Path.ContextName is not null)
        {
            return null;
        }

        var objectSelection = Visit(node.ObjectValueSelection);
        return objectSelection is null ? null : CreatePathSelection(node.Path, objectSelection);
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

        return selections.Count == 0
            ? null
            : new InlineFragmentNode(null, null, [], new SelectionSetNode(selections));
    }

    private static ISelectionNode? Visit(PathListValueSelectionNode node)
    {
        if (node.Path.ContextName is not null)
        {
            return null;
        }

        var listSelection = Visit(node.ListValueSelection);
        return listSelection is null ? null : CreatePathSelection(node.Path, listSelection);
    }

    private static ISelectionNode? Visit(ListValueSelectionNode node)
        => Visit(node.ElementSelection);
}

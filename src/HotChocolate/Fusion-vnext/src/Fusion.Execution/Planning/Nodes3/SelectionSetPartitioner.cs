using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public readonly ref struct SelectionSetPartitionerInput
{
    public required string SchemaName { get; init; }
    public required SelectionSet SelectionSet { get; init; }
    public required ISelectionSetIndex SelectionSetIndex { get; init; }
    public SelectionSetNode? ProvidedSelectionSetNode { get; init; }
    public bool AllowRequirements { get; init; }
}

public record SelectionSetPartitionerResult(
    SelectionSetNode? Resolvable,
    ImmutableStack<SelectionSet> Unresolvable,
    ImmutableStack<FieldSelection> FieldsWithRequirements,
    ISelectionSetIndex SelectionSetIndex);

public class SelectionSetPartitioner(CompositeSchema schema)
{
    public SelectionSetPartitionerResult Partition(
        SelectionSetPartitionerInput input)
    {
        var context = new Context
        {
            SchemaName = input.SchemaName,
            AllowRequirements = input.AllowRequirements,
            RootPath = input.SelectionSet.Path,
            SelectionSetIndex = input.SelectionSetIndex,
            Unresolvable = ImmutableStack<SelectionSet>.Empty
        };

        var (resolvable, _) =
            RewriteSelectionSet(
                context,
                input.SelectionSet.Type,
                input.SelectionSet.Node,
                input.ProvidedSelectionSetNode);

        return new SelectionSetPartitionerResult(
            resolvable,
            context.Unresolvable,
            context.SelectionSetIndex);
    }

    private (SelectionSetNode?, SelectionSetNode?) RewriteSelectionSet(
        Context context,
        ICompositeNamedType type,
        SelectionSetNode selectionSetNode,
        SelectionSetNode? providedSelectionSetNode)
    {
        var complexType = type as CompositeComplexType;
        List<ISelectionNode>? resolvableSelections = null;
        List<ISelectionNode>? unresolvableSelections = null;

        providedSelectionSetNode = GetProvidedSelectionSet(type, schema, providedSelectionSetNode);

        context.Nodes.Push(selectionSetNode);

        for (var i = 0; i < selectionSetNode.Selections.Count; i++)
        {
            var selection = selectionSetNode.Selections[i];

            switch (selection)
            {
                case FieldNode fieldNode:
                {
                    var (resolvable, unresolvable) =
                        RewriteFieldNode(
                            context,
                            complexType!,
                            fieldNode,
                            GetProvidedField(fieldNode, providedSelectionSetNode));

                    CompleteSelection(fieldNode, resolvable, unresolvable, i);
                    break;
                }

                case InlineFragmentNode inlineFragmentNode:
                {
                    var (resolvable, unresolvable) =
                        RewriteFragmentNode(
                            context,
                            type,
                            inlineFragmentNode,
                            providedSelectionSetNode);

                    CompleteSelection(inlineFragmentNode, resolvable, unresolvable, i);
                    break;
                }
            }
        }

        context.Nodes.Pop();

        if (resolvableSelections is null && unresolvableSelections is null)
        {
            return (selectionSetNode, null);
        }

        if (unresolvableSelections is not null && type.IsEntity())
        {
            var unresolvableSelectionSet = new SelectionSetNode(unresolvableSelections);
            context.Register(selectionSetNode, unresolvableSelectionSet);

            var selectionSet = new SelectionSet(
                context.GetId(selectionSetNode),
                unresolvableSelectionSet,
                type,
                context.BuildPath());
            context.Unresolvable = context.Unresolvable.Push(selectionSet);
            unresolvableSelections = null;
        }

        var result =
        (
            Resolvable:
                selectionSetNode.WithSelections(resolvableSelections
                    ?? selectionSetNode.Selections),
            Unresolvable: unresolvableSelections is not null
                ? selectionSetNode.WithSelections(unresolvableSelections)
                : null
        );

        context.Register(selectionSetNode, result.Resolvable);
        if(result.Unresolvable is not null)
        {
            context.Register(selectionSetNode, result.Unresolvable);
        }

        return result;

        void CompleteSelection<T>(T original, T? resolvable, T? unresolvable, int index) where T : ISelectionNode
        {
            if (resolvableSelections is null
                && (unresolvable is not null || !ReferenceEquals(resolvable, original)))
            {
                resolvableSelections ??= [];

                for (var j = 0; j < index; j++)
                {
                    resolvableSelections.Add(selectionSetNode.Selections[j]);
                }
            }

            if (unresolvable is not null)
            {
                unresolvableSelections ??= [];
                unresolvableSelections.Add(unresolvable);
            }

            if (resolvable is null)
            {
                return;
            }

            if (resolvableSelections is not null)
            {
                resolvableSelections.Add(resolvable);
            }
        }

        static FieldNode? GetProvidedField(FieldNode fieldNode, SelectionSetNode? providedSelectionSetNode)
        {
            if (providedSelectionSetNode is not null)
            {
                return providedSelectionSetNode.Selections
                    .OfType<FieldNode>()
                    .FirstOrDefault(t => t.Name.Value.Equals(fieldNode.Name.Value));
            }

            return null;
        }

        static SelectionSetNode? GetProvidedSelectionSet(
            ICompositeNamedType type,
            CompositeSchema schema,
            SelectionSetNode? providedSelectionSetNode)
        {
            // todo match correct inline fragment
            return providedSelectionSetNode;
        }
    }

    private (FieldNode?, FieldNode?) RewriteFieldNode(
        Context context,
        CompositeComplexType complexType,
        FieldNode fieldNode,
        FieldNode? providedFieldNode)
    {
        var field = complexType.Fields[fieldNode.Name.Value];

        if (providedFieldNode is null)
        {
            // if the field is not available in the current schema we return null
            // which will remove the field from the rewritten selection set.
            if (!field.Sources.TryGetMember(context.SchemaName, out var source))
            {
                return (null, fieldNode);
            }

            // if requirements are not allowed we return null
            // which will remove the field from the rewritten selection set.
            if (!context.AllowRequirements && source.Requirements is not null)
            {
                return (null, fieldNode);
            }
        }

        var selectionSet = fieldNode.SelectionSet;

        if (selectionSet is not null)
        {
            context.Nodes.Push(fieldNode);

            var (resolvable, unresolvable) = RewriteSelectionSet(
                context,
                field.Type.NamedType(),
                selectionSet,
                providedFieldNode?.SelectionSet);

            context.Nodes.Pop();

            if (!ReferenceEquals(resolvable, selectionSet))
            {
                return
                (
                    fieldNode.WithSelectionSet(resolvable),
                    unresolvable is null ? null : fieldNode.WithSelectionSet(unresolvable)
                );
            }
        }

        return (fieldNode, null);
    }

    private (InlineFragmentNode?, InlineFragmentNode?) RewriteFragmentNode(
        Context context,
        ICompositeNamedType type,
        InlineFragmentNode inlineFragmentNode,
        SelectionSetNode? providedFieldNode)
    {
        // TODO: we need to implement proper type routing here later.
        var typeCondition = type;

        if (inlineFragmentNode.TypeCondition is not null)
        {
            typeCondition = schema.GetType(inlineFragmentNode.TypeCondition.Name.Value);
        }

        context.Nodes.Push(inlineFragmentNode);

        var (resolvable, unresolvable) =
            RewriteSelectionSet(
                context,
                typeCondition,
                inlineFragmentNode.SelectionSet,
                providedFieldNode);

        context.Nodes.Pop();

        if (resolvable is null)
        {
            return (null, inlineFragmentNode);
        }

        return
        (
            inlineFragmentNode.WithSelectionSet(resolvable),
            unresolvable is null ? null : inlineFragmentNode.WithSelectionSet(unresolvable)
        );
    }

    private sealed class Context
    {
        private ISelectionSetIndex _selectionSetIndex = null!;
        private SelectionSetIndexBuilder? _selectionSetIndexBuilder;

        public required string SchemaName { get; init; }

        public required bool AllowRequirements { get; init; }

        public required SelectionPath RootPath { get; init; }

        public required ISelectionSetIndex SelectionSetIndex
        {
            get => _selectionSetIndex;
            init => _selectionSetIndex = value;
        }

        public SelectionSetIndexBuilder SelectionSetIndexBuilder
        {
            get
            {
                if (_selectionSetIndexBuilder is null)
                {
                    _selectionSetIndexBuilder = _selectionSetIndex.ToBuilder();
                    _selectionSetIndex = _selectionSetIndexBuilder;
                }

                return _selectionSetIndexBuilder;
            }
        }

        public required ImmutableStack<SelectionSet> Unresolvable { get; set; }

        public Stack<ISyntaxNode> Nodes { get; } = new();

        public SelectionPath BuildPath()
        {
            var path = RootPath;

            foreach (var node in Nodes)
            {
                switch (node)
                {
                    case FieldNode fieldNode:
                        path = path.AppendField(fieldNode.Name.Value);
                        break;

                    case InlineFragmentNode { TypeCondition: not null } inlineFragmentNode:
                        path = path.AppendFragment(inlineFragmentNode.TypeCondition.Name.Value);
                        break;
                }
            }

            return path;
        }

        public uint GetId(SelectionSetNode selectionSetNode)
            => _selectionSetIndex.GetId(selectionSetNode);

        public void Register(SelectionSetNode original, SelectionSetNode branch)
        {
            if (ReferenceEquals(original, branch))
            {
                return;
            }

            if(SelectionSetIndex.IsRegistered(branch))
            {
                return;
            }

            SelectionSetIndexBuilder.Register(original, branch);
        }
    }
}

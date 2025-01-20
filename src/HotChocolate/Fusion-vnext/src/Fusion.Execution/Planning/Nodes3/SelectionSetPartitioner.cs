using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public readonly ref struct SelectionSetPartitionerInput
{
    public required string SchemaName { get; init; }
    public bool AllowRequirements { get; init; }
    public required SelectionPath SelectionPath { get; init; }
    public required ICompositeNamedType Type { get; init; }
    public required SelectionSetNode SelectionSetNode { get; init; }
    public SelectionSetNode? ProvidedSelectionSetNode { get; init; }
}

public class SelectionSetPartitioner(CompositeSchema schema)
{
    public (SelectionSetNode?, SelectionSetNode?) Partition(
        SelectionSetPartitionerInput input,
        ref SelectionSetIndex selectionSetIndex,
        ref ImmutableStack<BacklogItem> backlog)
    {
        var context = new Context
        {
            SchemaName = input.SchemaName,
            AllowRequirements = input.AllowRequirements,
            RootPath = input.SelectionPath,
            SelectionSetIndex = selectionSetIndex,
            Backlog = backlog
        };

        var (resolvable, unresolvable) =
            RewriteSelectionSet(
                context,
                input.Type,
                input.SelectionSetNode,
                input.ProvidedSelectionSetNode);

        selectionSetIndex = context.SelectionSetIndex;
        backlog = context.Backlog;
        return (resolvable, unresolvable);
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
            context.RegisterSelectionSet(selectionSetNode, unresolvableSelectionSet);

            var workItem = new BacklogItem(
                PlanNodeKind.InlineLookupRequirements,
                context.BuildPath(),
                context.Nodes.Peek(),
                unresolvableSelectionSet,
                context.GetSelectionSetId(selectionSetNode),
                type);

            context.Backlog = context.Backlog.Push(workItem);
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

        context.RegisterSelectionSet(selectionSetNode, result.Resolvable);
        if(result.Unresolvable is not null)
        {
            context.RegisterSelectionSet(selectionSetNode, result.Unresolvable);
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
        private SelectionSetIndex _selectionSetIndex = null!;
        private bool _isIndexBranched;

        public required string SchemaName { get; init; }

        public required bool AllowRequirements { get; init; }

        public required SelectionPath RootPath { get; init; }

        public required SelectionSetIndex SelectionSetIndex
        {
            get => _selectionSetIndex;
            init => _selectionSetIndex = value;
        }

        public required ImmutableStack<BacklogItem> Backlog { get; set; }

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

        public int GetSelectionSetId(SelectionSetNode selectionSetNode)
            => _selectionSetIndex.GetSelectionSetId(selectionSetNode);

        public void RegisterSelectionSet(SelectionSetNode original, SelectionSetNode branch)
        {
            if (ReferenceEquals(original, branch))
            {
                return;
            }

            if(_selectionSetIndex.IsRegistered(branch))
            {
                return;
            }

            EnsureBranched();
            _selectionSetIndex.RegisterSelectionSet(original, branch);
        }

        private void EnsureBranched()
        {
            if (_isIndexBranched)
            {
                return;
            }

            _selectionSetIndex = _selectionSetIndex.Branch();
            _isIndexBranched = true;
        }
    }
}

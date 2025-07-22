using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndexer : SyntaxWalker
{
    private static readonly SelectionSetVisitor s_selectionSetVisitor = new();
    private readonly Dictionary<SelectionSetNode, uint> _selectionSetIds = [];
    private uint _nextId = 1;

    public static ISelectionSetIndex Create(OperationDefinitionNode operation)
    {
        var indexer = new SelectionSetIndexer();
        indexer.Visit(operation);
        return new SelectionSetIndex(indexer._selectionSetIds.ToImmutableDictionary(), indexer._nextId);
    }

    public static ImmutableHashSet<uint> CreateIdSet(SelectionSetNode selectionSet, ISelectionSetIndex index)
    {
        var context = new SelectionSetVisitor.Context(index);
        s_selectionSetVisitor.Visit(selectionSet, context);
        return context.SelectionSets.ToImmutable();
    }

    protected override ISyntaxVisitorAction Enter(SelectionSetNode node, object? context)
    {
        _selectionSetIds[node] = _nextId++;
        return base.Enter(node, context);
    }

    private sealed class SelectionSetVisitor : SyntaxWalker<SelectionSetVisitor.Context>
    {
        protected override ISyntaxVisitorAction Enter(SelectionSetNode node, Context context)
        {
            context.SelectionSets.Add(context.Index.GetId(node));
            return base.Enter(node, context);
        }

        public sealed class Context(ISelectionSetIndex index)
        {
            public ImmutableHashSet<uint>.Builder SelectionSets { get; } = ImmutableHashSet.CreateBuilder<uint>();

            public ISelectionSetIndex Index { get; } = index;
        }
    }
}

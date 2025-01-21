using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed class SelectionSetIndexer : SyntaxWalker
{
    private static readonly SelectionSetVisitor _selectionSetVisitor = new();
    private int _nextId = 1;
    private readonly Dictionary<SelectionSetNode, int> _selectionSetIds = new();

    public static ISelectionSetIndex Create(OperationDefinitionNode operation)
    {
        var indexer = new SelectionSetIndexer();
        indexer.Visit(operation);
        return new SelectionSetIndex(indexer._selectionSetIds.ToImmutableDictionary());
    }

    public static ImmutableHashSet<int> CreateIdSet(SelectionSetNode selectionSet, ISelectionSetIndex index)
    {
        var context = new SelectionSetVisitor.Context(index);
        _selectionSetVisitor.Visit(selectionSet, context);
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
            public ImmutableHashSet<int>.Builder SelectionSets { get; } = ImmutableHashSet.CreateBuilder<int>();

            public ISelectionSetIndex Index { get; } = index;
        }
    }
}

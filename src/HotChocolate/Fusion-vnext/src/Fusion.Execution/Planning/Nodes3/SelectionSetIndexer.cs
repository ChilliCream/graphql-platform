using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed class SelectionSetIndexer : SyntaxWalker
{
    private int _nextId = 1;
    private readonly Dictionary<SelectionSetNode, int> _selectionSetIds = new();

    public static SelectionSetIndex Create(OperationDefinitionNode operation)
    {
        var indexer = new SelectionSetIndexer();
        indexer.Visit(operation);
        return new SelectionSetIndex(indexer._selectionSetIds.ToImmutableDictionary());
    }

    protected override ISyntaxVisitorAction Enter(SelectionSetNode node, object? context)
    {
        _selectionSetIds[node] = _nextId++;
        return base.Enter(node, context);
    }
}

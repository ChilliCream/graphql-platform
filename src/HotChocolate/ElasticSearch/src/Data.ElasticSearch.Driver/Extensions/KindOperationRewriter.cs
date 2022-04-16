using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch.Filters;

internal class KindOperationRewriter : SearchOperationRewriter<ISearchOperation>
{
    private readonly ElasticSearchOperationKind _kind;

    private KindOperationRewriter(ElasticSearchOperationKind kind)
    {
        _kind = kind;
    }

    protected override ISearchOperation Rewrite(BoolOperation operation)
    {
        if (operation.Must.Count == 1 &&
            operation.Should.Count == 0 &&
            operation.Must[0] is BoolOperation)
        {
            return operation.Must[0];
        }

        return new BoolOperation(
            FilterOperations(operation.Must),
            FilterOperations(operation.Should));
    }

    protected override ISearchOperation Rewrite(MatchOperation operation) => operation;

    protected override ISearchOperation Rewrite(RangeOperation operation) => operation;

    protected override ISearchOperation Rewrite(TermOperation operation) => operation;

    private IReadOnlyList<ISearchOperation> FilterOperations(
        IReadOnlyList<ISearchOperation> operations)
    {
        if (operations.Count == 0)
        {
            return operations;
        }

        List<ISearchOperation> filteredOperations = new();

        foreach (var operation in operations)
        {
            if (operation is ILeafSearchOperation leaf)
            {
                if (leaf.Kind == _kind)
                {
                    filteredOperations.Add(Rewrite(operation));
                }
            }
            else
            {
                filteredOperations.Add(Rewrite(operation));
            }
        }

        return filteredOperations;
    }


    public static KindOperationRewriter Filter { get; } = new(ElasticSearchOperationKind.Filter);

    public static KindOperationRewriter Query { get; } = new(ElasticSearchOperationKind.Query);
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Data.ElasticSearch.Filters;

public static class ElasticSearchFilterScopeExtensions
{
    public static string GetPath(this ElasticSearchFilterScope scope) =>
        string.Join(".", scope.Path.Reverse());

    public static bool TryCreateQuery(
        this ElasticSearchFilterScope scope,
        [NotNullWhen(true)] out QueryDefinition? query)
    {
        query = null;

        if (scope.Level.Peek().Count == 0)
        {
            return false;
        }

        if (scope.Level.Peek().Peek() is not BoolOperation operation)
        {
            return false;
        }

        ISearchOperation[] queries = Array.Empty<ISearchOperation>();
        ISearchOperation[] filters = Array.Empty<ISearchOperation>();
        if (KindOperationRewriter.Query.Rewrite(operation) is BoolOperation rewrittenQuery &&
            rewrittenQuery is {Must.Count: > 0} or {Should.Count: > 0})
        {
            queries = new[] {rewrittenQuery};
        }

        if (KindOperationRewriter.Filter.Rewrite(operation) is BoolOperation rewrittenFilter &&
            rewrittenFilter is {Must.Count: > 0} or {Should.Count: > 0})
        {
            queries = new[] {rewrittenFilter};
        }

        query = new QueryDefinition(queries, filters);
        return true;
    }
}

public abstract class SearchOperationRewriter<T>
{
    public T Rewrite(ISearchOperation operation)
    {
        return operation switch
        {
            BoolOperation o => Rewrite(o),
            MatchOperation o => Rewrite(o),
            RangeOperation o => Rewrite(o),
            TermOperation o => Rewrite(o),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    protected abstract T Rewrite(BoolOperation operation);
    protected abstract T Rewrite(MatchOperation operation);
    protected abstract T Rewrite(RangeOperation operation);
    protected abstract T Rewrite(TermOperation operation);
}

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

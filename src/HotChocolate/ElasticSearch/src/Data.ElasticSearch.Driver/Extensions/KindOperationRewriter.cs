using System;
using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This rewriter rewrites an operation and splits <see cref="ElasticSearchOperationKind.Filter"/>
/// and <see cref="ElasticSearchOperationKind.Query"/> into "must" and "should"
/// </summary>
internal class KindOperationRewriter : SearchOperationRewriter<ISearchOperation?>
{
    private readonly ElasticSearchOperationKind _kind;

    /// <summary>
    /// Creates a new instance of <see cref="KindOperationRewriter"/>
    /// </summary>
    private KindOperationRewriter(ElasticSearchOperationKind kind)
    {
        _kind = kind;
    }

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(BoolOperation operation)
    {
        List<ISearchOperation>? must = null;
        List<ISearchOperation>? should = null;
        List<ISearchOperation>? mustNot = null;
        List<ISearchOperation>? filter = null;

        foreach (ISearchOperation mustOperation in operation.Must)
        {
            if (Rewrite(mustOperation) is not { } rewritten) continue;
            if (rewritten is BoolOperation boolOperation &&
                !boolOperation.Should.Any() &&
                !boolOperation.Filter.Any())
            {
                if (boolOperation.Must.Any())
                {
                    must ??= new List<ISearchOperation>();
                    must.AddRange(boolOperation.Must);
                }

                if (boolOperation.MustNot.Any())
                {
                    mustNot ??= new List<ISearchOperation>();
                    mustNot.AddRange(boolOperation.MustNot);
                }
            }
            else
            {
                must ??= new List<ISearchOperation>();
                must.Add(rewritten);
            }
        }

        foreach (ISearchOperation mustOperation in operation.Should)
        {
            if (Rewrite(mustOperation) is not { } rewritten) continue;
            if (rewritten is BoolOperation boolOperation &&
                !boolOperation.Must.Any() &&
                !boolOperation.MustNot.Any() &&
                !boolOperation.Filter.Any() &&
                boolOperation.Should.Any())
            {
                should ??= new List<ISearchOperation>();
                should.AddRange(boolOperation.Should);
            }
            else
            {
                should ??= new List<ISearchOperation>();
                should.Add(rewritten);
            }
        }

        foreach (ISearchOperation mustOperation in operation.MustNot)
        {
            if (Rewrite(mustOperation) is not { } rewritten) continue;
            if (rewritten is BoolOperation boolOperation &&
                !boolOperation.Should.Any() &&
                !boolOperation.Filter.Any())
            {
                if (boolOperation.MustNot.Any())
                {
                    must ??= new List<ISearchOperation>();
                    must.AddRange(boolOperation.MustNot);
                }

                if (boolOperation.Must.Any())
                {
                    mustNot ??= new List<ISearchOperation>();
                    mustNot.AddRange(boolOperation.Must);
                }
            }
            else
            {
                mustNot ??= new List<ISearchOperation>();
                mustNot.Add(rewritten);
            }
        }

        foreach (ISearchOperation mustOperation in operation.Filter)
        {
            if (Rewrite(mustOperation) is not { } rewritten) continue;
            if (rewritten is BoolOperation op &&
                !op.Must.Any() &&
                !op.MustNot.Any() &&
                !op.Filter.Any() &&
                !op.Should.Any())
            {
                filter ??= new List<ISearchOperation>();
                filter.AddRange(op.Filter);
            }
            else
            {
                filter ??= new List<ISearchOperation>();
                filter.Add(rewritten);
            }
        }

        if (must is null && mustNot is null && should is null && filter is null)
        {
            return null;
        }

        if (must is { Count: 1 } && mustNot is null && should is null && filter is null)
        {
            return must[0];
        }

        static IReadOnlyList<ISearchOperation> EnsureNotNull(
            IReadOnlyList<ISearchOperation>? operations)
            => operations ?? Array.Empty<ISearchOperation>();

        return new BoolOperation(
            EnsureNotNull(must),
            EnsureNotNull(should),
            EnsureNotNull(mustNot),
            EnsureNotNull(filter));
    }

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(MatchOperation operation)
        => RewriteLeaf(operation);

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(RangeOperation<double> operation)
        => RewriteLeaf(operation);

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(RangeOperation<long> operation)
        => RewriteLeaf(operation);

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(RangeOperation<DateTime> operation)
        => RewriteLeaf(operation);

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(TermOperation operation)
        => RewriteLeaf(operation);

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(WildCardOperation operation)
        => RewriteLeaf(operation);

    /// <inheritdoc />
    protected override ISearchOperation? Rewrite(ExistsOperation operation)
        => RewriteLeaf(operation);

    private ISearchOperation? RewriteLeaf(ILeafSearchOperation operation)
        => operation.Kind == _kind ? operation : null;

    /// <summary>
    /// Rewrites the operations and removes all operations that are not
    /// <see cref="ElasticSearchOperationKind.Filter"/>
    /// </summary>
    public static KindOperationRewriter Filter { get; } = new(ElasticSearchOperationKind.Filter);

    /// <summary>
    /// Rewrites the operations and removes all operations that are not
    /// <see cref="ElasticSearchOperationKind.Query"/>
    /// </summary>
    public static KindOperationRewriter Query { get; } = new(ElasticSearchOperationKind.Query);
}

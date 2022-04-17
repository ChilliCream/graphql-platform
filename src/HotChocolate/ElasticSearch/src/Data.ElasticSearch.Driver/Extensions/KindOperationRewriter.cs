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

    private KindOperationRewriter(ElasticSearchOperationKind kind)
    {
        _kind = kind;
    }

    protected override ISearchOperation? Rewrite(BoolOperation operation)
    {
        List<ISearchOperation>? must = null;
        List<ISearchOperation>? should = null;
        List<ISearchOperation>? mustNot = null;
        List<ISearchOperation>? filter = null;

        foreach (ISearchOperation mustOperation in operation.Must)
        {
            if (Rewrite(mustOperation) is { } rewritten)
            {
                if (rewritten is BoolOperation {Should.Count: 0, Filter.Count: 0} boolOperation)
                {
                    if (boolOperation.Must.Count > 0)
                    {
                        must ??= new List<ISearchOperation>();
                        must.AddRange(boolOperation.Must);
                    }

                    if (boolOperation.MustNot.Count > 0)
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
        }

        foreach (ISearchOperation mustOperation in operation.Should)
        {
            if (Rewrite(mustOperation) is { } rewritten)
            {
                if (rewritten is BoolOperation
                    {
                        Must.Count: 0,
                        MustNot.Count: 0,
                        Filter.Count: 0,
                        Should.Count: > 0
                    } boolOperation)
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
        }

        foreach (ISearchOperation mustOperation in operation.MustNot)
        {
            if (Rewrite(mustOperation) is { } rewritten)
            {
                if (rewritten is BoolOperation {Should.Count: 0, Filter.Count: 0} boolOperation)
                {
                    if (boolOperation.MustNot.Count > 0)
                    {
                        must ??= new List<ISearchOperation>();
                        must.AddRange(boolOperation.MustNot);
                    }

                    if (boolOperation.Must.Count > 0)
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
        }

        foreach (ISearchOperation mustOperation in operation.Filter)
        {
            if (Rewrite(mustOperation) is { } rewritten)
            {
                if (rewritten is BoolOperation {
                        Must.Count: 0,
                        MustNot.Count: 0,
                        Filter.Count: > 0,
                        Should.Count:  0
                    } op)
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
        }

        if (must is null && mustNot is null && should is null && filter is null)
        {
            return null;
        }

        if (must is {Count:1} && mustNot is null && should is null && filter is null)
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

    protected override ISearchOperation? Rewrite(MatchOperation operation)
        => operation.Kind == _kind ? operation : null;

    protected override ISearchOperation? Rewrite(RangeOperation operation)
        => operation.Kind == _kind ? operation : null;

    protected override ISearchOperation? Rewrite(TermOperation operation)
        => operation.Kind == _kind ? operation : null;

    public static KindOperationRewriter Filter { get; } = new(ElasticSearchOperationKind.Filter);

    public static KindOperationRewriter Query { get; } = new(ElasticSearchOperationKind.Query);
}

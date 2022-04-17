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
        List<ISearchOperation> must = new();
        List<ISearchOperation> should = new();
        List<ISearchOperation> mustNot = new();

        foreach (ISearchOperation mustOperation in operation.Must)
        {
            if (Rewrite(mustOperation) is { } rewritten)
            {
                if (rewritten is BoolOperation {Should.Count: 0} boolOperation)
                {
                    must.AddRange(boolOperation.Must);
                    mustNot.AddRange(boolOperation.MustNot);
                }
                else
                {
                    must.Add(rewritten);
                }
            }
        }

        foreach (ISearchOperation mustOperation in operation.Should)
        {
            if (Rewrite(mustOperation) is { } rewritten)
            {
                if (rewritten is BoolOperation {Must.Count: 0, MustNot.Count: 0} boolOperation)
                {
                    should.AddRange(boolOperation.Should);
                }
                else
                {
                    should.Add(rewritten);
                }
            }
        }

        foreach (ISearchOperation mustOperation in operation.MustNot)
        {
            if (Rewrite(mustOperation) is { } rewritten)
            {
                if (rewritten is BoolOperation {Should.Count: 0} boolOperation)
                {
                    must.AddRange(boolOperation.MustNot);
                    mustNot.AddRange(boolOperation.Must);
                }
                else
                {
                    mustNot.Add(rewritten);
                }
            }
        }

        if (must.Count == 0 && mustNot.Count == 0 && should.Count == 0)
        {
            return null;
        }

        return new BoolOperation(must, should, mustNot);
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

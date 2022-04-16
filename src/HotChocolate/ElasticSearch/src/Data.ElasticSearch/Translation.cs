using System.Linq;
using HotChocolate.Data.ElasticSearch.Filters;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Rewrites <see cref="ISearchOperation"/> to <see cref="IQuery"/>
/// </summary>
public class ElasticSearchOperationRewriter : SearchOperationRewriter<IQuery>
{
    /// <inheritdoc />
    protected override IQuery Rewrite(BoolOperation operation)
    {
        return new BoolQuery
        {
            Should = operation.Should
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x)),
            Must = operation.Must
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x)),
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(MatchOperation operation)
    {
        return new MatchQuery {Field = operation.Path, Query = operation.Value};
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(RangeOperation operation)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(TermOperation operation)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// The instance of the rewriter
    /// </summary>
    public static ElasticSearchOperationRewriter Instance { get; } = new();
}

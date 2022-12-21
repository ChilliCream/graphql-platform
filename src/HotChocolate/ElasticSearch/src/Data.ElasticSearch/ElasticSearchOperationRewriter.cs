using System;
using System.Collections.Generic;
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
        IEnumerable<QueryContainer> should = Array.Empty<QueryContainer>();
        IEnumerable<QueryContainer> must = Array.Empty<QueryContainer>();
        IEnumerable<QueryContainer> mustNot = Array.Empty<QueryContainer>();
        IEnumerable<QueryContainer> filter = Array.Empty<QueryContainer>();

        if (operation.Should.Count > 0)
        {
            should = operation.Should
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x));
        }

        if (operation.Must.Count > 0)
        {
            must = operation.Must
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x));
        }

        if (operation.MustNot.Count > 0)
        {
            mustNot = operation.MustNot
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x));
        }

        if (operation.Filter.Count > 0)
        {
            filter = operation.Filter
                .Select(Rewrite)
                .OfType<QueryBase>()
                .Select(x => new QueryContainer(x));
        }

        return new BoolQuery { Should = should, Must = must, MustNot = mustNot, Filter = filter };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(MatchOperation operation)
    {
        return new MatchQuery { Field = operation.Path, Query = operation.Value };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(RangeOperation<double> operation)
    {
        return new NumericRangeQuery
        {
            Field = operation.Path,
            GreaterThan = operation.GreaterThan?.Value,
            LessThan = operation.LowerThan?.Value,
            GreaterThanOrEqualTo = operation.GreaterThanOrEquals?.Value,
            LessThanOrEqualTo = operation.LowerThanOrEquals?.Value
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(RangeOperation<long> operation)
    {
        return new LongRangeQuery
        {
            Field = operation.Path,
            GreaterThan = operation.GreaterThan?.Value,
            LessThan = operation.LowerThan?.Value,
            GreaterThanOrEqualTo = operation.GreaterThanOrEquals?.Value,
            LessThanOrEqualTo = operation.LowerThanOrEquals?.Value
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(RangeOperation<string> operation)
    {
        return new TermRangeQuery
        {
            Field = operation.Path,
            GreaterThan = operation.GreaterThan?.Value,
            LessThan = operation.LowerThan?.Value,
            GreaterThanOrEqualTo = operation.GreaterThanOrEquals?.Value,
            LessThanOrEqualTo = operation.LowerThanOrEquals?.Value
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(RangeOperation<DateTime> operation)
    {
        return new DateRangeQuery
        {
            Field = operation.Path,
            GreaterThan = operation.GreaterThan?.Value,
            LessThan = operation.LowerThan?.Value,
            GreaterThanOrEqualTo = operation.GreaterThanOrEquals?.Value,
            LessThanOrEqualTo = operation.LowerThanOrEquals?.Value
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(TermOperation operation)
    {
        return new PrefixQuery {Field = operation.Path, Value = operation.Value};
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(ExistsOperation operation)
        => new ExistsQuery { Field = operation.Field };

    /// <summary>
    /// The instance of the rewriter
    /// </summary>
    public static ElasticSearchOperationRewriter Instance { get; } = new();
}

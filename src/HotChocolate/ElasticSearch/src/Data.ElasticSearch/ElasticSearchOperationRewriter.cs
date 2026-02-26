using System.Text.RegularExpressions;
using HotChocolate.Data.ElasticSearch.Filters;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Rewrites <see cref="ISearchOperation"/> to <see cref="IQuery"/>
/// </summary>
public class ElasticSearchOperationRewriter : SearchOperationRewriter<IQuery>
{
    private static Regex _escapeRegex = new(
        @"[+\-=!(){}\[\]^""~*?:\\/]",
        RegexOptions.Compiled);

    private static string EscapeForElasticSearch(string? value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : _escapeRegex.Replace(value, @"\$&");

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
            GreaterThan = operation.GreaterThan,
            LessThan = operation.LowerThan,
            GreaterThanOrEqualTo = operation.GreaterThanOrEquals,
            LessThanOrEqualTo = operation.LowerThanOrEquals
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(RangeOperation<long> operation)
    {
        return new LongRangeQuery
        {
            Field = operation.Path,
            GreaterThan = operation.GreaterThan,
            LessThan = operation.LowerThan,
            GreaterThanOrEqualTo = operation.GreaterThanOrEquals,
            LessThanOrEqualTo = operation.LowerThanOrEquals
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(RangeOperation<DateTime> operation)
    {
        return new DateRangeQuery
        {
            Field = operation.Path,
            GreaterThan = operation.GreaterThan,
            LessThan = operation.LowerThan,
            GreaterThanOrEqualTo = operation.GreaterThanOrEquals,
            LessThanOrEqualTo = operation.LowerThanOrEquals
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(TermOperation operation)
    {
        return new TermQuery { Field = operation.Path, Value = operation.Value };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(WildCardOperation operation)
    {
        var path = operation.Path.GetKeywordPath();
        var value = EscapeForElasticSearch(operation.Value);
        return operation.WildCardOperationKind switch
        {
            WildCardOperationKind.StartsWith => new QueryStringQuery { Query = $"{path}:{value}*" },
            WildCardOperationKind.EndsWith => new QueryStringQuery { Query = $"{path}:*{value}" },
            WildCardOperationKind.Contains => new QueryStringQuery { Query = $"{path}:*{value}*" },
            _ => throw new InvalidOperationException()
        };
    }

    /// <inheritdoc />
    protected override IQuery Rewrite(ExistsOperation operation)
        => new ExistsQuery { Field = operation.Path };

    /// <summary>
    /// The instance of the rewriter
    /// </summary>
    public static ElasticSearchOperationRewriter Instance { get; } = new();
}

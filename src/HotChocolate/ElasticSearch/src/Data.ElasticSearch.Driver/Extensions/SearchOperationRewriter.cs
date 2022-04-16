using System;

namespace HotChocolate.Data.ElasticSearch.Filters;

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

using System;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// Provides an abstract interface to rewriter <see cref="ISearchOperation"/>
/// </summary>
public abstract class SearchOperationRewriter<T>
{
    /// <summary>
    /// Rewrites <see cref="ISearchOperation"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="operation">The operation to rewrite</param>
    /// <returns>The rewritten operation</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Throws a <see cref="ArgumentOutOfRangeException"/> when the
    /// <paramref name="operation"/> is invalid
    /// </exception>
    public T Rewrite(ISearchOperation operation)
    {
        return operation switch
        {
            BoolOperation o => Rewrite(o),
            MatchOperation o => Rewrite(o),
            RangeOperation o => Rewrite(o),
            TermOperation o => Rewrite(o),
            WildcardOperation o => Rewrite(o),
            ExistsOperation o => Rewrite(o),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    /// <summary>
    /// Rewrites <see cref="BoolOperation"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="operation">The operation to rewrite</param>
    /// <returns>The rewritten operation</returns>
    protected abstract T Rewrite(BoolOperation operation);

    /// <summary>
    /// Rewrites <see cref="MatchOperation"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="operation">The operation to rewrite</param>
    /// <returns>The rewritten operation</returns>
    protected abstract T Rewrite(MatchOperation operation);

    /// <summary>
    /// Rewrites <see cref="RangeOperation"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="operation">The operation to rewrite</param>
    /// <returns>The rewritten operation</returns>
    protected abstract T Rewrite(RangeOperation operation);

    /// <summary>
    /// Rewrites <see cref="TermOperation"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="operation">The operation to rewrite</param>
    /// <returns>The rewritten operation</returns>
    protected abstract T Rewrite(TermOperation operation);

    /// <summary>
    /// Rewrites <see cref="WildcardOperation"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="operation">The operation to rewrite</param>
    /// <returns>The rewritten operation</returns>
    protected abstract T Rewrite(WildcardOperation operation);

    /// <summary>
    /// Rewrites <see cref="ExistsOperation"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="operation">The operation to rewrite</param>
    /// <returns>The rewritten operation</returns>
    protected abstract T Rewrite(ExistsOperation operation);
}

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using HotChocolate.Pagination;

namespace GreenDonut.Projections;

public static class BatchingDataLoaderExtensions
{
    public static IPagingDataLoader<TKey, TValue> WithPagingArguments<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        PagingArguments pagingArguments)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        var branchKey = CreateBranchKey(pagingArguments);
        return (IPagingDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, CreatePagingDataLoader, pagingArguments);

        static IDataLoader CreatePagingDataLoader(
            string branchKey,
            IDataLoader<TKey, TValue> root,
            PagingArguments pagingArguments)
        {
            return new PagingDataLoader<TKey, TValue>(
                (DataLoaderBase<TKey, TValue>)root,
                branchKey)
            {
                ContextData =
                {
                    { typeof(PagingArguments).FullName!, pagingArguments }
                }
            };
        }
    }

#if NET8_0_OR_GREATER
    [Experimental(Experiments.Projections)]
#endif
    public static IPagingDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IPagingDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TValue>> selector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var builder = dataLoader.GetOrSetState(_ => new DefaultSelectorBuilder<TValue>());
        builder.Add(selector);
        return dataLoader;
    }

    private static string CreateBranchKey(
        PagingArguments pagingArguments)
    {
        var key = new StringBuilder();
        key.Append(pagingArguments.First);
        key.Append(pagingArguments.After);
        key.Append(pagingArguments.Last);
        key.Append(pagingArguments.Before);
        return key.ToString();
    }
}

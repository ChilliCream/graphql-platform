#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Projections;

[Experimental(Experimentals.Projections)]
public static class SelectionDataLoaderExtensions
{
    public static IDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
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

        DefaultSelectorContext<TValue> context;
        if (dataLoader.ContextData.TryGetValue(typeof(ISelectorContext).FullName!, out var value)
            && value is DefaultSelectorContext<TValue> casted)
        {
            context = casted;
        }
        else
        {
            context = new DefaultSelectorContext<TValue>();
        }

        context.Add(selector);
        dataLoader.ContextData = dataLoader.ContextData.SetItem(typeof(ISelectorContext).FullName!, context);
        return dataLoader;
    }

    public static IQueryable<T> Select<T>(
        this IQueryable<T> queryable,
        ISelectorContext context)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var selector = context.TryCompile<T>();

        if (selector is null)
        {
            return queryable;
        }

        return queryable.Select(selector);
    }
}
#endif

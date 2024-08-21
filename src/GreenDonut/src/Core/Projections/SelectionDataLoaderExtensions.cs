#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Projections;

[Experimental(Experiments.Projections)]
public static class SelectionDataLoaderExtensions
{
    public static ISelectionDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TValue>> selector)
        where TKey : notnull
        where TValue : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        DefaultSelectorBuilder<TValue> context;
        var branch = dataLoader.Branch(selector.ToString());
        if (branch.ContextData.TryGetValue(typeof(ISelectorBuilder).FullName!, out var value)
            && value is DefaultSelectorBuilder<TValue> casted)
        {
            context = casted;
        }
        else
        {
            context = new DefaultSelectorBuilder<TValue>();
        }

        context.Add(selector);
        branch.ContextData = branch.ContextData.SetItem(typeof(ISelectorBuilder).FullName!, context);
        return branch;
    }

    public static ISelectionDataLoader<TKey, TValue> Select<TKey, TValue>(
        this ISelectionDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TValue>> selector)
        where TKey : notnull
        where TValue : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var context = (DefaultSelectorBuilder<TValue>)dataLoader.ContextData[typeof(ISelectorBuilder).FullName!]!;
        context.Add(selector);
        return dataLoader;
    }

    public static ISelectorQuery<T> Select<T>(
        this IQueryable<T> queryable,
        ISelectorBuilder builder)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var selector = builder.TryCompile<T>();
        return new DefaultSelectorQuery<T>(queryable, selector);
    }
}
#endif

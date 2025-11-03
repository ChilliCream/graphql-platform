// ReSharper disable once CheckNamespace
using System.Linq.Expressions;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods for the <see cref="QueryContext{TEntity}"/>.
/// </summary>
public static class GreenDonutQueryContextExtensions
{
    public static QueryContext<TEntity> Include<TEntity, TValue>(
        this QueryContext<TEntity> context,
        Expression<Func<TEntity, TValue>>? propertySelector)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (propertySelector is null)
        {
            return context;
        }

        var normalizedSelector = ExpressionHelpers.Rewrite(propertySelector);

        if (context.Selector is null)
        {
            return context with
            {
                Selector = normalizedSelector
            };
        }
        else
        {
            return context with
            {
                Selector = ExpressionHelpers.Combine(context.Selector, normalizedSelector)
            };
        }
    }

    public static QueryContext<TEntity> Select<TEntity>(
        this QueryContext<TEntity> context,
        Expression<Func<TEntity, TEntity>>? selector)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (selector is null)
        {
            return context;
        }

        if (context.Selector is null)
        {
            return context with
            {
                Selector = selector
            };
        }
        else
        {
            return context with
            {
                Selector = ExpressionHelpers.Combine(context.Selector, selector)
            };
        }
    }
}

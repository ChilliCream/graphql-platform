using System.Linq.Expressions;

namespace HotChocolate.Pagination.Expressions;

internal static class QueryHelpers
{
    public static IQueryable<T> EnsureOrderPropsAreSelected<T>(
        IQueryable<T> query)
    {
        var selector = ExtractCurrentSelector(query);
        if (selector is null)
        {
            return query;
        }

        var orderByProperties = ExtractOrderProperties(query);
        if(orderByProperties.Count == 0)
        {
            return query;
        }

        var updatedSelector = AddPropertiesInSelector(selector, orderByProperties);
        return ReplaceSelector(query, updatedSelector);
    }

    private static Expression<Func<T, T>>? ExtractCurrentSelector<T>(
        IQueryable<T> query)
    {
        var visitor = new ExtractSelectExpressionVisitor();
        visitor.Visit(query.Expression);
        return visitor.Selector as Expression<Func<T, T>>;
    }

    private static Expression<Func<T, T>> AddPropertiesInSelector<T>(
        Expression<Func<T, T>> selector,
        List<MemberExpression> properties)
    {
        var parameter = selector.Parameters[0];
        var bindings = ((MemberInitExpression)selector.Body).Bindings.Cast<MemberAssignment>().ToList();

        foreach (var property in properties)
        {
            var propertyName = property.Member.Name;
            if(property.Expression is not ParameterExpression parameterExpression
                || bindings.Any(b => b.Member.Name == propertyName))
            {
                continue;
            }

            var replacer = new ReplacerParameterVisitor(parameterExpression, parameter);
            var rewrittenProperty = (MemberExpression)replacer.Visit(property);
            bindings.Add(Expression.Bind(rewrittenProperty.Member, rewrittenProperty));
        }

        var newBody = Expression.MemberInit(Expression.New(typeof(T)), bindings);
        return Expression.Lambda<Func<T, T>>(newBody, parameter);
    }

    private static List<MemberExpression> ExtractOrderProperties<T>(
        IQueryable<T> query)
    {
        var visitor = new ExtractOrderPropertiesVisitor();
        visitor.Visit(query.Expression);
        return visitor.OrderProperties;
    }

    private static IQueryable<T> ReplaceSelector<T>(
        IQueryable<T> query,
        Expression<Func<T, T>> newSelector)
    {
        var visitor = new ReplaceSelectorVisitor<T>(newSelector);
        var newExpression = visitor.Visit(query.Expression);
        return query.Provider.CreateQuery<T>(newExpression);
    }
}

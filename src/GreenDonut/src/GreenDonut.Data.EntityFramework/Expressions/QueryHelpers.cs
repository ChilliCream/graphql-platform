using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

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
        if (orderByProperties.Count == 0)
        {
            return query;
        }

        var updatedSelector = AddPropertiesInSelector(selector, orderByProperties);
        return ReplaceSelector(query, updatedSelector);
    }

    public static IQueryable<T> EnsureGroupPropsAreSelected<T, TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector)
    {
        var selector = ExtractCurrentSelector(query);
        if (selector is null)
        {
            return query;
        }

        var properties = GetMemberExpressions(keySelector);
        if (properties.Count == 0)
        {
            return query;
        }

        var updatedSelector = AddPropertiesInSelector(selector, properties);
        return ReplaceSelector(query, updatedSelector);

        static List<MemberExpression> GetMemberExpressions(Expression<Func<T, TKey>> keySelector)
        {
            var properties = new List<MemberExpression>();
            var body = keySelector.Body;

            if (body is UnaryExpression unaryExpr)
            {
                body = unaryExpr.Operand;
            }

            if (body is not MemberExpression memberExpr)
            {
                return properties;
            }

            // traverse the member expression chain to extract properties required for grouping
            while (memberExpr.Expression is MemberExpression parentMember)
            {
                properties.Add(parentMember);
                memberExpr = parentMember;
            }

            // if no navigation properties are found, and we have a root-level property
            if (properties.Count == 0 && memberExpr.Expression is ParameterExpression)
            {
                properties.Add(memberExpr);
            }

            return properties;
        }
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
        var visitor = new AddPropertiesVisitorRewriter(properties, parameter);
        var updatedBody = visitor.Visit(selector.Body);
        return Expression.Lambda<Func<T, T>>(updatedBody, parameter);
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

    public class AddPropertiesVisitorRewriter : ExpressionVisitor
    {
        private readonly List<MemberExpression> _propertiesToAdd;
        private readonly ParameterExpression _parameter;

        public AddPropertiesVisitorRewriter(
            List<MemberExpression> propertiesToAdd,
            ParameterExpression parameter)
        {
            _propertiesToAdd = propertiesToAdd;
            _parameter = parameter;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            // Get existing bindings (properties in the current selector)
            var existingBindings = node.Bindings.Cast<MemberAssignment>().ToList();

            // Add the properties that are not already present in the bindings
            foreach (var property in _propertiesToAdd)
            {
                var propertyName = property.Member.Name;
                if (property.Expression is ParameterExpression parameterExpression
                    && existingBindings.All(b => b.Member.Name != propertyName))
                {
                    var replacer = new ReplacerParameterVisitor(parameterExpression, _parameter);
                    var rewrittenProperty = (MemberExpression)replacer.Visit(property);
                    existingBindings.Add(Expression.Bind(rewrittenProperty.Member, rewrittenProperty));
                }
            }

            // Create new MemberInitExpression with updated bindings
            return Expression.MemberInit(node.NewExpression, existingBindings);
        }
    }
}

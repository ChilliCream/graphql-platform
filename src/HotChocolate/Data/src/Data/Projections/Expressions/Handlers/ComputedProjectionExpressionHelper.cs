using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Data.ExpressionUtils;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public static class ComputedProjectionExpressionHelper
{
    public const string ComputedProjectionKey = "ComputedProjection";

    public sealed class ComputedProjection
    {
        public ComputedProjection(LambdaExpression expression)
        {
            Expression = expression;
        }

        public LambdaExpression Expression { get; }
        // public List<string> Dependencies { get; }
    }

    public static void SetComputedProjection(
        this IDictionary<string, object?> contextData,
        ComputedProjection computedProjection)
    {
        contextData[ComputedProjectionKey] = computedProjection;
    }

    public static ComputedProjection? GetComputedProjection(this IObjectField field)
    {
        if (field.ContextData.TryGetValue(ComputedProjectionKey, out var computedProjection))
        {
            return (ComputedProjection) computedProjection!;
        }
        return null;
    }

    public static bool HasComputedProjection(this IObjectField field)
    {
        return field.ContextData.ContainsKey(ComputedProjectionKey);
    }

    public static bool CanBeUsedInProjection(this IObjectField field)
    {
        return field.Member is not null || field.HasComputedProjection();
    }

    public static Expression GetProjectionExpression(
        this IObjectField field,
        Expression instance)
    {
        Expression result;
        if (field.GetComputedProjection() is { } computedProjection)
        {
            result = computedProjection.Expression
                .ReplaceParameterAndGetBody(instance);
        }
        else
        {
            result = instance.Append(field.Member);
        }

        return result;
    }

    public static Expression MaybeCastValueTypeToObject(
        this Expression expression)
    {
        if (expression.Type.IsValueType)
            return Expression.Convert(expression, typeof(object));
        return expression;
    }
}

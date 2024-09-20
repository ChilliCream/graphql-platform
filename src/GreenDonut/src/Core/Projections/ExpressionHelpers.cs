#if NET6_0_OR_GREATER
using System.Linq.Expressions;
using System.Reflection;

namespace GreenDonut.Projections;

internal static class ExpressionHelpers
{
    public static Expression<Func<T, T>> Combine<T>(
        Expression<Func<T, T>> first,
        Expression<Func<T, T>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var firstBody = (MemberInitExpression)ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = (MemberInitExpression)ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var combinedBody = CombineMemberInitExpressions(firstBody, secondBody);
        return Expression.Lambda<Func<T, T>>(combinedBody, parameter);
    }

    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression toReplace,
        Expression replacement)
        => new ParameterReplacer(toReplace, replacement).Visit(body);

    private static MemberInitExpression CombineMemberInitExpressions(
        MemberInitExpression first,
        MemberInitExpression second)
    {
        var bindings = new Dictionary<string, MemberAssignment>();

        foreach (var binding in first.Bindings.Cast<MemberAssignment>())
        {
            bindings[binding.Member.Name] = binding;
        }

        foreach (var binding in second.Bindings.Cast<MemberAssignment>())
        {
            bindings[binding.Member.Name] = binding;
        }

        return Expression.MemberInit(first.NewExpression, bindings.Values);
    }

    public static Expression<Func<TRoot, TRoot>> Rewrite<TRoot>(
        Expression<Func<TRoot, object?>> selector)
    {
        var parameter = selector.Parameters[0];
        var bindings = new List<MemberBinding>();

        if (selector.Body is NewExpression newExpression)
        {
            foreach (var argument in newExpression.Arguments)
            {
                var memberExpression = ExtractMemberExpression(argument);
                if (memberExpression != null)
                {
                    bindings.Add(CreateBinding(parameter, memberExpression));
                }
            }
        }
        else if (selector.Body is MemberExpression memberExpression)
        {
            bindings.Add(CreateBinding(parameter, memberExpression));
        }
        else if (selector.Body is UnaryExpression { Operand: MemberExpression unaryMemberExpression })
        {
            bindings.Add(CreateBinding(parameter, unaryMemberExpression));
        }
        else
        {
            throw new InvalidOperationException("Unsupported selector format.");
        }

        var newInitExpression = Expression.MemberInit(Expression.New(typeof(TRoot)), bindings);
        return Expression.Lambda<Func<TRoot, TRoot>>(newInitExpression, parameter);
    }

    private static MemberBinding CreateBinding(ParameterExpression parameter, MemberExpression memberExpression)
    {
        if (memberExpression.Expression != parameter)
        {
            throw new InvalidOperationException("Nested expressions are not supported.");
        }

        var property = (PropertyInfo)memberExpression.Member;
        return Expression.Bind(property, memberExpression);
    }

    private static MemberExpression? ExtractMemberExpression(
        Expression expression)
        => expression switch
        {
            MemberExpression memberExpression => memberExpression,
            UnaryExpression { Operand: MemberExpression operand } => operand,
            _ => null
        };

    private sealed class ParameterReplacer(ParameterExpression toReplace, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == toReplace ? replacement : base.VisitParameter(node);
    }
}
#endif

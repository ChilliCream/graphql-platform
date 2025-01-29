using System.Linq.Expressions;
using System.Reflection;

namespace GreenDonut;

internal static class ExpressionHelpers
{
    public static Expression<Func<T, T>> Combine<T>(
        Expression<Func<T, T>> first,
        Expression<Func<T, T>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "root");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var combinedBody = CombineExpressions(firstBody, secondBody);
        return Expression.Lambda<Func<T, T>>(combinedBody, parameter);
    }

    public static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var combinedBody = Expression.AndAlso(firstBody, secondBody);
        return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
    }

    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression toReplace,
        Expression replacement)
        => new ParameterReplacer(toReplace, replacement).Visit(body);

    private static Expression CombineExpressions(Expression first, Expression second)
    {
        if (first is UnaryExpression { NodeType: ExpressionType.Convert } firstUnary)
        {
            return CombineWithConvertExpression(firstUnary, second);
        }

        if (first is MemberInitExpression firstInit && second is MemberInitExpression secondInit)
        {
            return CombineMemberInitExpressions(firstInit, secondInit);
        }

        if (first is ConditionalExpression firstCond && second is ConditionalExpression secondCond)
        {
            return CombineConditionalExpressions(firstCond, secondCond);
        }

        if (first is ConditionalExpression firstConditional && second is MemberInitExpression secondMemberInit)
        {
            return CombineConditionalAndMemberInit(firstConditional, secondMemberInit);
        }

        if (first is MemberInitExpression firstMemberInit && second is ConditionalExpression secondConditional)
        {
            return CombineConditionalAndMemberInit(secondConditional, firstMemberInit);
        }

        // as a fallback we return the second body, assuming it overwrites the first.
        return second;
    }


    private static Expression CombineWithConvertExpression(UnaryExpression first, Expression second)
    {
        if (second is MemberInitExpression otherMemberInit)
        {
            var combinedInit = CombineMemberInitExpressions((MemberInitExpression)first.Operand, otherMemberInit);
            return Expression.Convert(combinedInit, first.Type);
        }

        if (second is ConditionalExpression otherConditional)
        {
            var combinedCond = CombineConditionalExpressions((ConditionalExpression)first.Operand, otherConditional);
            return Expression.Convert(combinedCond, first.Type);
        }

        return Expression.Convert(second, first.Type);
    }

    private static MemberInitExpression CombineMemberInitExpressions(
        MemberInitExpression first,
        MemberInitExpression second)
    {
        var bindings = new Dictionary<string, MemberAssignment>();

        foreach (var binding in first.Bindings.Cast<MemberAssignment>())
        {
            bindings[binding.Member.Name] = binding;
        }

        var firstRootExpression = ExtractRootExpressionFromBindings(first.Bindings.Cast<MemberAssignment>());
        var parameterToReplace = ExtractParameterExpression(firstRootExpression);

        if (firstRootExpression != null && parameterToReplace != null)
        {
            var replacer = new RootExpressionReplacerVisitor(parameterToReplace, firstRootExpression);

            foreach (var binding in second.Bindings.Cast<MemberAssignment>())
            {
                var newBindingExpression = replacer.Visit(binding.Expression);
                bindings[binding.Member.Name] = Expression.Bind(binding.Member, newBindingExpression);
            }
        }
        else
        {
            foreach (var binding in second.Bindings.Cast<MemberAssignment>())
            {
                bindings[binding.Member.Name] = binding;
            }
        }

        return Expression.MemberInit(first.NewExpression, bindings.Values);
    }

    private static Expression? ExtractRootExpressionFromBindings(
        IEnumerable<MemberAssignment> bindings)
        => bindings.FirstOrDefault()?.Expression is MemberExpression memberExpr
            ? memberExpr.Expression
            : null;

    private static ParameterExpression? ExtractParameterExpression(Expression? expression)
        => expression switch
        {
            UnaryExpression { NodeType: ExpressionType.Convert } expr => ExtractParameterExpression(expr.Operand),
            ParameterExpression paramExpr => paramExpr,
            _ => null
        };

    private sealed class RootExpressionReplacerVisitor(
        ParameterExpression parameterToReplace,
        Expression replacementExpression)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == parameterToReplace)
            {
                return replacementExpression;
            }
            return base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var expr = Visit(node.Expression);
            if (expr != node.Expression)
            {
                return Expression.MakeMemberAccess(expr, node.Member);
            }
            return base.VisitMember(node);
        }
    }

    private static ConditionalExpression CombineConditionalExpressions(
        ConditionalExpression first,
        ConditionalExpression second)
    {
        var test = first.Test;
        var ifTrue = CombineExpressions(first.IfTrue, second.IfTrue);
        var ifFalse = CombineExpressions(first.IfFalse, second.IfFalse);
        return Expression.Condition(test, ifTrue, ifFalse);
    }

    private static Expression CombineConditionalAndMemberInit(
        ConditionalExpression condition,
        MemberInitExpression memberInit)
    {
        var ifTrue = CombineExpressions(condition.IfTrue, memberInit);

        var ifFalse = condition.IfFalse is ConstantExpression
            ? condition.IfFalse
            : CombineExpressions(condition.IfFalse, memberInit);

        return Expression.Condition(condition.Test, ifTrue, ifFalse);
    }

    public static Expression<Func<TRoot, TRoot>> Rewrite<TRoot, TKey>(
        Expression<Func<TRoot, TKey?>> selector)
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

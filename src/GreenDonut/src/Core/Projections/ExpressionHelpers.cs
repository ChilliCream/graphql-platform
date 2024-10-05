using System.Linq.Expressions;
using System.Reflection;

namespace GreenDonut.Projections;

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

    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression toReplace,
        Expression replacement)
        => new ParameterReplacer(toReplace, replacement).Visit(body);

    private static Expression CombineExpressions(Expression first, Expression second)
    {
        // Handle Convert expressions
        if (first is UnaryExpression firstUnary && firstUnary.NodeType == ExpressionType.Convert)
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
        // Check if we are combining with another MemberInitExpression
        if (second is MemberInitExpression otherMemberInit)
        {
            var combinedInit = CombineMemberInitExpressions((MemberInitExpression)first.Operand, otherMemberInit);
            return Expression.Convert(combinedInit, first.Type);
        }

        // Check if we are combining with a ConditionalExpression
        if (second is ConditionalExpression otherConditional)
        {
            var combinedCond = CombineConditionalExpressions((ConditionalExpression)first.Operand, otherConditional);
            return Expression.Convert(combinedCond, first.Type);
        }

        // Fallback if it's another kind of expression
        return Expression.Convert(second, first.Type);
    }

   private static MemberInitExpression CombineMemberInitExpressions(
    MemberInitExpression first,
    MemberInitExpression second)
{
    var bindings = new Dictionary<string, MemberAssignment>();

    // Collect bindings from the first expression
    foreach (var binding in first.Bindings.Cast<MemberAssignment>())
    {
        bindings[binding.Member.Name] = binding;
    }

    // Extract the root expression from the first expression
    var firstRootExpression = ExtractRootExpressionFromBindings(first.Bindings.Cast<MemberAssignment>());
    var parameterToReplace = ExtractParameterExpression(firstRootExpression);

    // Adjust the second expression's bindings
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
        // If unable to extract, use the original bindings
        foreach (var binding in second.Bindings.Cast<MemberAssignment>())
        {
            bindings[binding.Member.Name] = binding;
        }
    }

    // Create a new MemberInitExpression with the combined bindings
    return Expression.MemberInit(first.NewExpression, bindings.Values);
}

private static Expression ExtractRootExpressionFromBindings(IEnumerable<MemberAssignment> bindings)
{
    var firstBinding = bindings.FirstOrDefault();
    if (firstBinding?.Expression is MemberExpression memberExpr)
    {
        return memberExpr.Expression;
    }
    return null;
}

private static ParameterExpression ExtractParameterExpression(Expression expression)
{
    if (expression is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
    {
        return ExtractParameterExpression(unaryExpr.Operand);
    }
    else if (expression is ParameterExpression paramExpr)
    {
        return paramExpr;
    }
    else
    {
        return null;
    }
}

class RootExpressionReplacerVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _parameterToReplace;
    private readonly Expression _replacementExpression;

    public RootExpressionReplacerVisitor(ParameterExpression parameterToReplace, Expression replacementExpression)
    {
        _parameterToReplace = parameterToReplace;
        _replacementExpression = replacementExpression;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _parameterToReplace)
        {
            return _replacementExpression;
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

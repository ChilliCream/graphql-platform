using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Sorting.Expressions;

internal static class QueryableSortExpressionOptimizer
{
    private static readonly PropertyInfo s_dateTimeOffsetDateTime =
        typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.DateTime))!;

    public static bool TryRewriteSelectorToSource(
        Expression source,
        ParameterExpression selectorParameter,
        Expression selector,
        [NotNullWhen(true)] out Expression? rewrittenSource,
        [NotNullWhen(true)] out LambdaExpression? rewrittenSelector,
        [NotNullWhen(true)] out LambdaExpression? projection)
    {
        rewrittenSource = null;
        rewrittenSelector = null;
        projection = null;

        if (source is not MethodCallExpression selectCall
            || !IsSelectMethod(selectCall.Method)
            || selectCall.Arguments.Count != 2
            || TryExtractLambda(selectCall.Arguments[1]) is not { Parameters.Count: 1 } selectLambda
            || selectLambda.ReturnType != selectorParameter.Type)
        {
            return false;
        }

        if (!TryRewriteProjectedExpression(
            selector,
            selectorParameter,
            selectLambda.Body,
            out var sourceSelector))
        {
            return false;
        }

        sourceSelector = DateTimeOffsetDateTimeExpressionVisitor.Rewrite(sourceSelector);
        rewrittenSource = selectCall.Arguments[0];
        rewrittenSelector = Expression.Lambda(sourceSelector, selectLambda.Parameters[0]);
        projection = selectLambda;
        return true;
    }

    public static Expression ReapplyProjection(
        Expression source,
        LambdaExpression projection)
        => Expression.Call(
            source.GetEnumerableKind(),
            nameof(Queryable.Select),
            [projection.Parameters[0].Type, projection.ReturnType],
            source,
            projection);

    private static bool IsSelectMethod(MethodInfo method)
        => method.Name.Equals(nameof(Queryable.Select), StringComparison.Ordinal)
            && (method.DeclaringType == typeof(Queryable)
                || method.DeclaringType == typeof(Enumerable));

    private static LambdaExpression? TryExtractLambda(Expression expression)
        => expression switch
        {
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda } => lambda,
            LambdaExpression lambda => lambda,
            _ => null
        };

    private static bool TryRewriteProjectedExpression(
        Expression expression,
        ParameterExpression selectorParameter,
        Expression projection,
        [NotNullWhen(true)] out Expression? rewritten)
    {
        if (expression == selectorParameter)
        {
            rewritten = projection;
            return true;
        }

        if (expression is MemberExpression memberExpression)
        {
            if (memberExpression.Expression is null
                || !TryRewriteProjectedExpression(
                    memberExpression.Expression,
                    selectorParameter,
                    projection,
                    out var parentExpression)
                || !TryBindMember(parentExpression, memberExpression.Member, out rewritten))
            {
                rewritten = null;
                return false;
            }

            return true;
        }

        if (expression is UnaryExpression unaryExpression
            && (unaryExpression.NodeType == ExpressionType.Convert
                || unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            if (!TryRewriteProjectedExpression(
                unaryExpression.Operand,
                selectorParameter,
                projection,
                out var operand))
            {
                rewritten = null;
                return false;
            }

            rewritten = Expression.MakeUnary(
                unaryExpression.NodeType,
                operand,
                unaryExpression.Type,
                unaryExpression.Method);
            return true;
        }

        if (!ParameterExpressionVisitor.Contains(expression, selectorParameter))
        {
            rewritten = expression;
            return true;
        }

        rewritten = null;
        return false;
    }

    private static bool TryBindMember(
        Expression source,
        MemberInfo member,
        [NotNullWhen(true)] out Expression? rewritten)
    {
        if (source is MemberInitExpression memberInit)
        {
            foreach (var binding in memberInit.Bindings)
            {
                if (binding is MemberAssignment assignment
                    && binding.Member.Name.Equals(member.Name, StringComparison.Ordinal))
                {
                    rewritten = assignment.Expression;
                    return true;
                }
            }

            rewritten = null;
            return false;
        }

        if (source is NewExpression { Members: not null } newExpression)
        {
            for (var i = 0; i < newExpression.Members!.Count; i++)
            {
                if (newExpression.Members[i].Name.Equals(member.Name, StringComparison.Ordinal))
                {
                    rewritten = newExpression.Arguments[i];
                    return true;
                }
            }

            rewritten = null;
            return false;
        }

        if (member.DeclaringType?.IsAssignableFrom(source.Type) ?? false)
        {
            rewritten = Expression.MakeMemberAccess(source, member);
            return true;
        }

        rewritten = null;
        return false;
    }

    private sealed class DateTimeOffsetDateTimeExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member == s_dateTimeOffsetDateTime
                && node.Expression is not null
                && node.Expression.Type == typeof(DateTimeOffset))
            {
                return Visit(node.Expression);
            }

            return base.VisitMember(node);
        }

        public static Expression Rewrite(Expression expression)
            => new DateTimeOffsetDateTimeExpressionVisitor().Visit(expression);
    }

    private sealed class ParameterExpressionVisitor(ParameterExpression parameter) : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter = parameter;

        public bool ContainsParameter { get; private set; }

        public override Expression? Visit(Expression? node)
        {
            if (ContainsParameter || node is null)
            {
                return node;
            }

            return base.Visit(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            ContainsParameter = node == _parameter;
            return node;
        }

        public static bool Contains(Expression expression, ParameterExpression parameter)
        {
            var visitor = new ParameterExpressionVisitor(parameter);
            visitor.Visit(expression);
            return visitor.ContainsParameter;
        }
    }
}

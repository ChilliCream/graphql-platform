using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Sorting.Expressions;

internal static class QueryableSortExpressionOptimizer
{
    private static readonly PropertyInfo s_dateTimeOffsetDateTime =
        typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.DateTime))!;

    /// <summary>
    /// Tries to push a sort selector through a <c>.Select(...)</c> projection so that sorting
    /// happens on the original source instead of the projected type. This allows the database
    /// to apply the sort before the projection, which produces more efficient SQL.
    /// </summary>
    /// <param name="source">The query expression, expected to be a <c>.Select(...)</c> call.</param>
    /// <param name="selectorParameter">The parameter the sort selector operates on (the projected type).</param>
    /// <param name="selector">The sort selector expression to rewrite.</param>
    /// <param name="rewrittenSource">The original source before the <c>.Select(...)</c>, if successful.</param>
    /// <param name="rewrittenSelector">The sort selector rewritten to operate on the original source type, if successful.</param>
    /// <param name="projection">The original <c>.Select(...)</c> lambda so it can be re-applied after sorting, if successful.</param>
    /// <returns><c>true</c> if the rewrite succeeded; otherwise <c>false</c>.</returns>
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

        // We only proceed if the source is a .Select() call whose projection produces the same type
        // that the sort selector operates on. Anything else can't be optimized.
        if (source is not MethodCallExpression selectCall
            || !IsSelectMethod(selectCall.Method)
            || selectCall.Arguments.Count != 2
            || TryExtractLambda(selectCall.Arguments[1]) is not { Parameters.Count: 1 } selectLambda
            || selectLambda.ReturnType != selectorParameter.Type)
        {
            return false;
        }

        // Next, we try to trace the sort expression back through the projection to find the equivalent
        // expression on the original source. If we can't, there is nothing we can do here.
        if (!TryRewriteProjectedExpression(
            selector,
            selectorParameter,
            selectLambda.Body,
            out var sourceSelector))
        {
            return false;
        }

        // Finally, we strip any .DateTime access off DateTimeOffset fields so the sort translates
        // cleanly to SQL, then package everything up and return success.
        sourceSelector = DateTimeOffsetDateTimeExpressionVisitor.Rewrite(sourceSelector);
        rewrittenSource = selectCall.Arguments[0];
        rewrittenSelector = Expression.Lambda(sourceSelector, selectLambda.Parameters[0]);
        projection = selectLambda;
        return true;
    }

    /// <summary>
    /// Re-applies the original <c>.Select(...)</c> projection on top of <paramref name="source"/>
    /// after the sort has been pushed down to the underlying source query.
    /// </summary>
    /// <param name="source">The sorted source expression to project over.</param>
    /// <param name="projection">The original Select lambda captured from <see cref="TryRewriteSelectorToSource"/>.</param>
    /// <returns>A new expression equivalent to <c>source.Select(projection)</c>.</returns>
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

        // We handle member access (e.g. dto.Name) by recursively rewriting the parent expression
        // and then looking up which source expression was assigned to that member in the projection.
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

        // Next, we handle type casts by rewriting the inner operand and then rebuilding the cast
        // around the rewritten expression.
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
        // We first check for object initializer expressions (new Foo { Name = ... }) and look
        // for a binding that matches the member name.
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

        // Next, we check for constructor expressions (new Foo(...)) and match the member by name
        // against the constructor parameters.
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

        // Finally, if the source type directly exposes the member, we just access it directly.
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

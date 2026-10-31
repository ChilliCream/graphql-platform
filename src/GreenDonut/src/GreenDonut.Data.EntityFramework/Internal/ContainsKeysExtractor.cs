using System.Linq.Expressions;

namespace GreenDonut.Data.Internal;

/// <summary>
/// Finds the in-memory key collection behind a <c>keys.Contains(keySelector(x))</c>-shaped
/// predicate already present in a queryable's expression tree. The collection operand is
/// compiled and invoked, and must be evaluable in memory without a database round trip.
/// </summary>
internal static class ContainsKeysExtractor
{
    /// <summary>
    /// Tries to find a <c>Contains</c> call that is a top-level <c>&amp;&amp;</c> conjunct of a
    /// <c>Where</c> predicate on <paramref name="expression"/>'s own source chain, whose tested
    /// value is <paramref name="keySelector"/> applied to the query's own parameter. A
    /// <c>Contains</c> call nested under a negation, a <c>||</c>, or another lambda never
    /// matches.
    /// </summary>
    /// <param name="expression">
    /// The queryable's expression tree to search.
    /// </param>
    /// <param name="keySelector">
    /// Selects the key a matching <c>Contains</c> call must test.
    /// </param>
    /// <param name="keys">
    /// The collection found, or null if no matching call was found.
    /// </param>
    /// <typeparam name="TElement">
    /// The type of the queryable's elements.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of the key.
    /// </typeparam>
    /// <returns>
    /// Returns true if a matching call was found.
    /// </returns>
    public static bool TryExtract<TElement, TKey>(
        Expression expression,
        Expression<Func<TElement, TKey>> keySelector,
        out IReadOnlyCollection<TKey>? keys)
    {
        var finder = new Finder<TElement, TKey>(keySelector);
        finder.Walk(expression);
        keys = finder.Keys;
        return keys is not null;
    }

    private sealed class Finder<TElement, TKey>(Expression<Func<TElement, TKey>> keySelector)
    {
        public IReadOnlyCollection<TKey>? Keys { get; private set; }

        public void Walk(Expression expression)
        {
            var current = expression;

            while (Keys is null && current is MethodCallExpression { Arguments.Count: > 0 } call)
            {
                if (call.Method.DeclaringType == typeof(Queryable)
                    && call.Method.Name == nameof(Queryable.Where)
                    && Unquote(call.Arguments[1]) is LambdaExpression predicate)
                {
                    foreach (var conjunct in FlattenAndAlso(predicate.Body))
                    {
                        if (conjunct is MethodCallExpression { Method.Name: "Contains" } containsCall
                            && TryMatch(containsCall, out var found))
                        {
                            Keys = found;
                            break;
                        }
                    }
                }

                current = call.Arguments[0];
            }
        }

        private static IEnumerable<Expression> FlattenAndAlso(Expression expression)
        {
            if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso)
            {
                foreach (var left in FlattenAndAlso(andAlso.Left))
                {
                    yield return left;
                }

                foreach (var right in FlattenAndAlso(andAlso.Right))
                {
                    yield return right;
                }
            }
            else
            {
                yield return expression;
            }
        }

        private static Expression Unquote(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression;
        }

        private bool TryMatch(MethodCallExpression node, out IReadOnlyCollection<TKey>? keys)
        {
            keys = null;
            Expression collectionExpr;
            Expression itemExpr;

            if (node.Object is not null && node.Arguments.Count == 1)
            {
                collectionExpr = node.Object;
                itemExpr = node.Arguments[0];
            }
            else if (node.Object is null && node.Arguments.Count == 2)
            {
                collectionExpr = node.Arguments[0];
                itemExpr = node.Arguments[1];
            }
            else
            {
                return false;
            }

            if (itemExpr.Type != typeof(TKey))
            {
                return false;
            }

            var rootParameter = FindRootParameter(itemExpr);

            if (rootParameter is null || rootParameter.Type != typeof(TElement))
            {
                return false;
            }

            var reboundBody = new RebindVisitor(keySelector.Parameters[0], rootParameter).Visit(keySelector.Body);

            if (reboundBody.ToString() != itemExpr.ToString())
            {
                return false;
            }

            // Discards a span conversion around an array-literal collection operand.
            if (collectionExpr is MethodCallExpression
                { Method: { Name: "op_Implicit" or "op_Explicit", IsStatic: true }, Arguments.Count: 1 } conversion)
            {
                collectionExpr = conversion.Arguments[0];
            }

            if (!typeof(IEnumerable<TKey>).IsAssignableFrom(collectionExpr.Type)
                || ReferencesParameter(collectionExpr))
            {
                return false;
            }

            // The whole collection operand is compiled as a standalone, parameterless delegate
            // and invoked directly.
            var operand = collectionExpr.Type == typeof(IEnumerable<TKey>)
                ? collectionExpr
                : Expression.Convert(collectionExpr, typeof(IEnumerable<TKey>));
            var enumerable = Expression.Lambda<Func<IEnumerable<TKey>>>(operand).Compile()();

            keys = enumerable.Distinct().ToArray();
            return true;
        }

        private static bool ReferencesParameter(Expression expression)
        {
            var visitor = new ParameterReferenceVisitor();
            visitor.Visit(expression);
            return visitor.Found;
        }

        private static ParameterExpression? FindRootParameter(Expression expression)
        {
            var current = expression;

            while (true)
            {
                switch (current)
                {
                    case ParameterExpression parameter:
                        return parameter;
                    case MemberExpression { Expression: { } inner }:
                        current = inner;
                        continue;
                    case UnaryExpression
                    {
                        NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
                    } unary:
                        current = unary.Operand;
                        continue;
                    default:
                        return null;
                }
            }
        }

        private sealed class RebindVisitor(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
                => node == from ? to : base.VisitParameter(node);
        }

        // Flags a parameter reference not bound by a lambda inside the collection expression
        // itself.
        private sealed class ParameterReferenceVisitor : ExpressionVisitor
        {
            private readonly HashSet<ParameterExpression> _bound = [];

            public bool Found { get; private set; }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                foreach (var parameter in node.Parameters)
                {
                    _bound.Add(parameter);
                }

                return base.VisitLambda(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (!_bound.Contains(node))
                {
                    Found = true;
                }

                return base.VisitParameter(node);
            }
        }
    }
}

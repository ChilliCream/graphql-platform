using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Data.Cursors;

/// <summary>
/// This expression visitor traverses a query expression, collects cursor keys, and removes OrderBy nodes.
/// If a cursor key cannot be generated, the OrderBy is still removed.
/// </summary>
public sealed class CursorKeyExtractor : ExpressionVisitor
{
    private readonly List<CursorKey> _keys = [];

    public IReadOnlyList<CursorKey> Keys => _keys;

    protected override Expression VisitExtension(Expression node)
        => node.CanReduce ? base.VisitExtension(node) : node;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsOrderBy(node) || IsThenBy(node))
        {
            PushProperty(node);
            return Visit(node.Arguments[0]);
        }
        else if (IsOrderByDescending(node) || IsThenByDescending(node))
        {
            PushProperty(node, CursorKeyDirection.Descending);
            return Visit(node.Arguments[0]);
        }

        return base.VisitMethodCall(node);
    }

    private static bool IsOrderBy(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.OrderBy), typeof(Queryable))
            || IsMethod(node, nameof(Enumerable.OrderBy), typeof(Enumerable));

    private static bool IsThenBy(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.ThenBy), typeof(Queryable))
            || IsMethod(node, nameof(Enumerable.ThenBy), typeof(Enumerable));

    private static bool IsOrderByDescending(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.OrderByDescending), typeof(Queryable))
            || IsMethod(node, nameof(Enumerable.OrderByDescending), typeof(Enumerable));

    private static bool IsThenByDescending(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.ThenByDescending), typeof(Queryable))
            || IsMethod(node, nameof(Enumerable.ThenByDescending), typeof(Enumerable));

    private static bool IsMethod(MethodCallExpression node, string name, Type declaringType)
        => node.Method.DeclaringType == declaringType && node.Method.Name.Equals(name, StringComparison.Ordinal);

    private void PushProperty(MethodCallExpression node, CursorKeyDirection direction = CursorKeyDirection.Ascending)
    {
        if (TryExtractProperty(node, out var expression))
        {
            var serializer = CursorKeySerializerRegistration.Find(expression.ReturnType);
            _keys.Insert(0, new CursorKey(expression, serializer, direction));
        }
    }

    private static bool TryExtractProperty(
        MethodCallExpression node,
        [NotNullWhen(true)] out LambdaExpression? expression)
    {
        if (node.Arguments is [_, UnaryExpression { Operand: LambdaExpression l }])
        {
            expression = l;
            return true;
        }

        if (node.Arguments is [_, LambdaExpression l1])
        {
            expression = l1;
            return true;
        }

        expression = null;
        return false;
    }
}

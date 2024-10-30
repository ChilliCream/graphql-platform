using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Pagination.Expressions;

/// <summary>
/// This expression visitor will collect the cursor keys from a query expression.
/// </summary>
public sealed class CursorKeyParser : ExpressionVisitor
{
    private readonly List<CursorKey> _keys = new();

    public IReadOnlyList<CursorKey> Keys => _keys;

    protected override Expression VisitExtension(Expression node)
        => node.CanReduce ? base.VisitExtension(node) : node;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsOrderBy(node))
        {
            PushProperty(node);
        }
        else if (IsThenBy(node))
        {
            PushProperty(node);
        }
        else if (IsOrderByDescending(node))
        {
            PushProperty(node, CursorKeyDirection.Descending);
        }
        else if (IsThenByDescending(node))
        {
            PushProperty(node, CursorKeyDirection.Descending);
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

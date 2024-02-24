using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data;

internal sealed class DataSetKeyParser : ExpressionVisitor
{
    private readonly List<DataSetKey> _keys = new();

    public IReadOnlyList<DataSetKey> Keys => _keys;

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
            PushProperty(node, false);
        }
        else if (IsThenByDescending(node))
        {
            PushProperty(node, false);
        }

        return base.VisitMethodCall(node);
    }

    private static bool IsOrderBy(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.OrderBy), typeof(Queryable)) ||
            IsMethod(node, nameof(Enumerable.OrderBy), typeof(Enumerable));
    
    private static bool IsThenBy(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.ThenBy), typeof(Queryable)) ||
            IsMethod(node, nameof(Enumerable.ThenBy), typeof(Enumerable));
    
    private static bool IsOrderByDescending(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.OrderByDescending), typeof(Queryable)) ||
            IsMethod(node, nameof(Enumerable.OrderByDescending), typeof(Enumerable));
    
    private static bool IsThenByDescending(MethodCallExpression node)
        => IsMethod(node, nameof(Queryable.ThenByDescending), typeof(Queryable)) ||
            IsMethod(node, nameof(Enumerable.ThenByDescending), typeof(Enumerable));
    
    private static bool IsMethod(MethodCallExpression node, string name, Type declaringType)
        => node.Method.DeclaringType == declaringType &&
            node.Method.Name.Equals(name, StringComparison.Ordinal);
    
    private void PushProperty(MethodCallExpression node, bool ascending = true)
    {
        if (TryExtractProperty(node, out var property))
        {
            _keys.Insert(0, new DataSetKey(property, ascending));   
        }
    }
    
    private static bool TryExtractProperty(
        MethodCallExpression node,
        [NotNullWhen(true)] out PropertyInfo? property)
    {
        if (node.Arguments.Count == 2 &&
            node.Arguments[1] is UnaryExpression u &&
            u.Operand is LambdaExpression l &&
            l.Body is MemberExpression m &&
            m.Member is PropertyInfo p)
        {
            property = p;
            return true;
        }
        
        if (node.Arguments.Count == 2 &&
            node.Arguments[1] is LambdaExpression l1 &&
            l1.Body is MemberExpression m1 &&
            m1.Member is PropertyInfo p1)
        {
            property = p1;
            return true;
        }

        property = null;
        return false;
    }
}
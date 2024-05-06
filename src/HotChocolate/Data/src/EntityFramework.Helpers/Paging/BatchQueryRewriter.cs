using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Pagination;

namespace HotChocolate.Data;

internal sealed class BatchQueryRewriter<T>(PagingArguments arguments) : ExpressionVisitor
{
    private PropertyInfo? _resultProperty;
    private DataSetKey[]? _keys;

    public PropertyInfo ResultProperty => _resultProperty ?? throw new InvalidOperationException();

    public DataSetKey[] Keys => _keys ?? throw new InvalidOperationException();

    protected override Expression VisitExtension(Expression node)
        => node.CanReduce
            ? base.VisitExtension(node)
            : node;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsInclude(node) && TryExtractProperty(node, out var property) && _resultProperty is null)
        {
            _resultProperty = property;
            var newIncludeExpression = RewriteInclude(node, property);
            return base.VisitMethodCall(newIncludeExpression);
        }

        return base.VisitMethodCall(node);
    }

    private MethodCallExpression RewriteInclude(MethodCallExpression node, PropertyInfo property)
    {
        var forward = arguments.Last is null;

        var entityType = node.Arguments[0].Type.GetGenericArguments()[0];
        var includeType = property.PropertyType.GetGenericArguments()[0];
        var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;

        var parser = new DataSetKeyParser();
        parser.Visit(lambda);
        var keys = _keys = parser.Keys.ToArray();

        var pagingExpr = ApplyPaging(lambda.Body, arguments, keys, forward);
        var newLambda = Expression.Lambda(pagingExpr, lambda.Parameters);
        return Expression.Call(null, Include(), node.Arguments[0], Expression.Constant(newLambda));

        MethodInfo Include()
            => typeof(EntityFrameworkQueryableExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(t => t.Name.Equals("Include") && t.GetGenericArguments().Length == 2)
                .MakeGenericMethod(entityType, typeof(IEnumerable<>).MakeGenericType(includeType));
    }

    private static Expression ApplyPaging(
        Expression enumerable,
        PagingArguments pagingArgs,
        DataSetKey[] keys,
        bool forward)
    {
        MethodInfo? where = null;
        MethodInfo? take = null;

        if (pagingArgs.After is not null)
        {
            var cursor = CursorParser.Parse(pagingArgs.After, keys);
            enumerable = Expression.Call(
                null,
                Where(),
                enumerable,
                PagingQueryableExtensions.BuildWhereExpression<T>(keys, cursor, forward));
        }

        if (pagingArgs.Before is not null)
        {
            var cursor = CursorParser.Parse(pagingArgs.Before, keys);
            enumerable = Expression.Call(
                null,
                Where(),
                enumerable,
                PagingQueryableExtensions.BuildWhereExpression<T>(keys, cursor, forward));
        }

        if (pagingArgs.First is not null)
        {
            var first = Expression.Constant(pagingArgs.First.Value);
            enumerable = Expression.Call(null, Take(), enumerable, first);
        }

        if (pagingArgs.Last is not null)
        {
            var last = Expression.Constant(pagingArgs.Last.Value);
            enumerable = Expression.Call(null, Take(), enumerable, last);
        }

        return enumerable;

        MethodInfo Where()
            => where ??= typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(t => t.Name.Equals("Where") && t.GetGenericArguments().Length == 1)
                .MakeGenericMethod(typeof(T));

        MethodInfo Take()
            => take ??= typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(t => t.Name.Equals("Take") && t.GetGenericArguments().Length == 1)
                .MakeGenericMethod(typeof(T));
    }

    private static bool IsInclude(MethodCallExpression node)
        => IsMethod(node, nameof(EntityFrameworkQueryableExtensions.Include));

    private static bool IsMethod(MethodCallExpression node, string name)
        => node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
            node.Method.Name.Equals(name, StringComparison.Ordinal);

    private static bool TryExtractProperty(
        MethodCallExpression node,
        [NotNullWhen(true)] out PropertyInfo? property)
    {
        if (node.Arguments is [_, UnaryExpression { Operand: LambdaExpression l }])
        {
            return TryExtractProperty1(l.Body, out property);
        }

        property = null;
        return false;
    }

    private static bool TryExtractProperty1(Expression expression, out PropertyInfo? property)
    {
        property = null;

        switch (expression)
        {
            case MemberExpression memberExpression:
                property = memberExpression.Member as PropertyInfo;
                return property != null;

            case MethodCallExpression methodCallExpression:
            {
                if (methodCallExpression.Arguments.Count > 0)
                {
                    var firstArgument = methodCallExpression.Arguments[0];

                    switch (firstArgument)
                    {
                        case MethodCallExpression:
                            return TryExtractProperty1(firstArgument, out property);

                        case UnaryExpression unaryExpression:
                            return TryExtractProperty1(unaryExpression.Operand, out property);

                        case MemberExpression:
                            return TryExtractProperty1(firstArgument, out property);
                    }
                }
                break;
            }

            case UnaryExpression unaryExpression:
                return TryExtractProperty1(unaryExpression.Operand, out property);
        }

        return false;
    }
}

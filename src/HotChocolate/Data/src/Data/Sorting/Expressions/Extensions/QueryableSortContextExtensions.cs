using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions;

public static class QueryableSortVisitorContextExtensions
{
    public static IQueryable<TSource> Sort<TSource>(
        this QueryableSortContext context,
        IQueryable<TSource> source)
    {
        if (context.Operations.Count == 0)
        {
            return source;
        }

        return source.Provider.CreateQuery<TSource>(context.Compile(source.Expression));
    }

    public static Expression Compile(
        this QueryableSortContext context,
        Expression source)
    {
        if (context.Operations.Count == 0)
        {
            return source;
        }

        var firstOperation = true;

        foreach (var operation in context.Operations)
        {
            if (firstOperation &&
                !OrderingMethodFinder.OrderMethodExists(source))
            {
                source = operation.CompileOrderBy(source);
            }
            else
            {
                source = operation.CompileThenBy(source);
            }

            firstOperation = false;
        }

        return source;
    }

    // Adapted from internal System.Web.Util.OrderingMethodFinder
    // http://referencesource.microsoft.com/#System.Web/Util/OrderingMethodFinder.cs
    private sealed class OrderingMethodFinder : ExpressionVisitor
    {
        private bool _orderingMethodFound = false;

        public override Expression? Visit(Expression? node)
        {
            if (_orderingMethodFound)
            {
                return node;
            }

            return base.Visit(node);
        }

        protected override Expression VisitExtension(Expression node) => node.CanReduce ? base.VisitExtension(node) : node;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var name = node.Method.Name;

            if (node.Method.DeclaringType == typeof(Queryable) && (
                name.StartsWith(nameof(Queryable.OrderBy), StringComparison.Ordinal) ||
                name.StartsWith(nameof(Queryable.ThenBy), StringComparison.Ordinal)))
            {
                _orderingMethodFound = true;
            }

            return base.VisitMethodCall(node);
        }

        public static bool OrderMethodExists(Expression expression)
        {
            var visitor = new OrderingMethodFinder();
            visitor.Visit(expression);
            return visitor._orderingMethodFound;
        }
    }
}

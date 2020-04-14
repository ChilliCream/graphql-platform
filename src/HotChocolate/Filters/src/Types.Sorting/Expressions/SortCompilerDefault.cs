using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting.Expressions
{
    public static class SortCompilerDefault
    {
        public static Expression Compile(
            SortingExpressionVisitorDefinition visitorDefinition,
            QueryableSortVisitorContext context,
            Expression source)
        {
            if (context.SortOperations.Count == 0)
            {
                return source;
            }

            if (!OrderingMethodFinder.OrderMethodExists(source))
            {
                source = source.CompileInitialSortOperation(
                    context.SortOperations.Dequeue());
            }

            while (context.SortOperations.Count != 0)
            {
                source = source.CompileSortOperation(
                    context.SortOperations.Dequeue());
            }

            return source;
        }

        // Adapted from internal System.Web.Util.OrderingMethodFinder
        // http://referencesource.microsoft.com/#System.Web/Util/OrderingMethodFinder.cs
        private class OrderingMethodFinder : ExpressionVisitor
        {
            private bool _orderingMethodFound = false;

            public override Expression Visit(Expression node)
            {
                if (_orderingMethodFound)
                {
                    return node;
                }
                return base.Visit(node);
            }

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
}
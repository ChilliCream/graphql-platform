using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Types.Sorting
{
    public class QueryableSortVisitorContext
        : SortVisitorContextBase
    {
        private const string _parameterName = "t";

        private readonly bool _inMemory;

        public QueryableSortVisitorContext(
            InputObjectType initialType,
            Type source,
            bool inMemory)
            : base(initialType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            Closure = new SortQueryableClosure(source, _parameterName);
            _inMemory = inMemory;
        }

        public SortQueryableClosure Closure { get; }

        public Stack<SortOperationInvocation> SortOperations { get; } =
            new Stack<SortOperationInvocation>();

        public virtual SortOperationInvocation CreateSortOperation(
            SortOperationKind kind)
        {
            if (_inMemory)
            {
                return Closure.CreateInMemorySortOperation(kind);
            }
            return Closure.CreateSortOperation(kind);
        }

        public IQueryable<TSource> Sort<TSource>(
            IQueryable<TSource> source)
        {
            if (SortOperations.Count == 0)
            {
                return source;
            }

            return source.Provider.CreateQuery<TSource>(Compile(source.Expression));
        }

        private Expression Compile(
            Expression source)
        {
            if (SortOperations.Count == 0)
            {
                return source;
            }

            if (!OrderingMethodFinder.OrderMethodExists(source))
            {
                source = source.CompileInitialSortOperation(
                    SortOperations.Pop());
            }

            while (SortOperations.Count != 0)
            {
                source = source.CompileSortOperation(
                    SortOperations.Pop());
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

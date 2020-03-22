using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Sorting
{
    public class QueryableSortVisitor
            : SortVisitorBase
    {
        private const string _parameterName = "t";

        public QueryableSortVisitor(
            InputObjectType initialType,
            Type source) : base(initialType)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }

            Closure = new SortQueryableClosure(source, _parameterName);
        }

        protected Queue<SortOperationInvocation> SortOperations { get; } =
            new Queue<SortOperationInvocation>();

        protected SortQueryableClosure Closure { get; }

        protected virtual SortOperationInvocation CreateSortOperation(SortOperationKind kind)
        {
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

        public Expression Compile(
            Expression source)
        {
            if (SortOperations.Count == 0)
            {
                return source;
            }

            if (!OrderingMethodFinder.OrderMethodExists(source))
            {
                source = source.CompileInitialSortOperation(
                    SortOperations.Dequeue());
            }

            while (SortOperations.Count != 0)
            {
                source = source.CompileSortOperation(
                    SortOperations.Dequeue());
            }

            return source;
        }

        #region Object Value

        public override VisitorAction Enter(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        #endregion

        #region Object Field

        public override VisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            base.Enter(node, parent, path, ancestors);

            if (Operations.Peek() is SortOperationField sortField)
            {
                Closure.EnqueueProperty(sortField.Operation.Property);
                if (!sortField.Operation.IsObject)
                {
                    var kind = (SortOperationKind)sortField.Type.Deserialize(node.Value.Value);
                    SortOperations.Enqueue(CreateSortOperation(kind));
                }
            }

            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {

            if (Operations.Peek() is SortOperationField)
            {
                Closure.Pop();
            }
            return base.Leave(node, parent, path, ancestors);
        }

        #endregion

        #region List

        public override VisitorAction Enter(
            ListValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        #endregion

        // Adapted from internal System.Web.Util.OrderingMethodFinder
        // http://referencesource.microsoft.com/#System.Web/Util/OrderingMethodFinder.cs
        private class OrderingMethodFinder : ExpressionVisitor
        {
            bool _orderingMethodFound = false;

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

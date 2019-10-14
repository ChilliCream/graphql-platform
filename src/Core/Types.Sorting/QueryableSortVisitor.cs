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


        public IQueryable<TSource> Sort<TSource>(
            IQueryable<TSource> source)
        {
            if (!SortOperations.Any())
            {
                return source;
            }

            IOrderedQueryable<TSource> sortedSource
                = source.AddInitialSortOperation(
                    SortOperations.Dequeue());

            while (SortOperations.Any())
            {
                sortedSource
                    = sortedSource.AddSortOperation(
                        SortOperations.Dequeue());
            }

            return sortedSource;
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
                    SortOperations.Enqueue(
                           Closure.CreateSortOperation(kind)
                    );

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
                Closure.Instance.Pop();
            }
            return VisitorAction.Continue;
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Sorting
{
    public class QueryableSortVisitor
            : SyntaxNodeVisitor
    {
        private readonly ParameterExpression _parameter;

        public QueryableSortVisitor(
            InputObjectType initialType,
            Type source)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }

            Types.Push(initialType);
            _parameter = Expression.Parameter(source);
        }

        protected Queue<SortOperationInvocation> Instance { get; } =
            new Queue<SortOperationInvocation>();

        protected Stack<IType> Types { get; } =
            new Stack<IType>();

        public IQueryable<TSource> Sort<TSource>(
            IQueryable<TSource> source)
        {
            if (!Instance.Any())
            {
                return source;
            }

            IOrderedQueryable<TSource> sortedSource
                = source.AddInitialSortOperation(
                    Instance.Dequeue(), _parameter);

            while (Instance.Any())
            {
                sortedSource
                    = sortedSource.AddSortOperation(
                        Instance.Dequeue(), _parameter);
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
            if (!(Types.Peek().NamedType() is InputObjectType inputType))
            {
                // TODO : resources - invalid type
                throw new NotSupportedException();
            }

            if (!inputType.Fields.TryGetField(node.Name.Value,
                out IInputField field))
            {
                // TODO : resources - invalid field
                throw new InvalidOperationException();
            }

            if (field is SortOperationField sortField)
            {
                Instance.Enqueue(
                    new SortOperationInvocation(
                        (SortOperationKind) sortField.Type.Deserialize(node.Value.Value),
                        sortField.Operation.Property));
            }

            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
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

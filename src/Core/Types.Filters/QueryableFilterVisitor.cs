using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitor
        : FilterVisitorBase
    {
        private readonly QueryableFilterVisitorContext _context;

        public QueryableFilterVisitor(
            InputObjectType initialType,
            Type source,
            ITypeConversion converter,
            bool inMemory)
            : this(
                initialType,
                source,
                converter,
                ExpressionOperationHandlers.All,
                ExpressionFieldHandlers.All,
                inMemory)
        {
        }

        public QueryableFilterVisitor(
            InputObjectType initialType,
            Type source,
            ITypeConversion converter,
            IEnumerable<IExpressionOperationHandler> operationHandlers,
            IEnumerable<IExpressionFieldHandler> fieldHandlers,
            bool inMemory)
            : base(initialType)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (operationHandlers is null)
            {
                throw new ArgumentNullException(nameof(operationHandlers));
            }
            if (converter is null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            _context = new QueryableFilterVisitorContext(
                operationHandlers.ToArray(),
                fieldHandlers.ToArray(),
                converter,
                new QueryableClosure(source, "r", inMemory),
                inMemory);
        }

        public Expression<Func<TSource, bool>> CreateFilter<TSource>()
        {
            return _context.GetClosure().CreateLambda<Func<TSource, bool>>();
        }

        #region Object Value
        public override VisitorAction Enter(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _context.PushLevel(new Queue<Expression>());
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Queue<Expression> operations = _context.PopLevel();

            if (TryCombineOperations(
                operations,
                (a, b) => Expression.AndAlso(a, b),
                out Expression combined))
            {
                _context.GetLevel().Enqueue(combined);
            }

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

            if (Operations.Peek() is FilterOperationField field)
            {
                for (var i = _context.FieldHandlers.Count - 1; i >= 0; i--)
                {
                    if (_context.FieldHandlers[i].Enter(
                        _context,
                        field,
                        node,
                        parent,
                        path,
                        ancestors,
                        out VisitorAction action))
                    {
                        return action;
                    }
                }
                for (var i = _context.OperationHandlers.Count - 1; i >= 0; i--)
                {
                    if (_context.OperationHandlers[i].TryHandle(
                        _context,
                        field.Operation,
                        field.Type,
                        node.Value,
                        _context.GetInstance(),
                        out Expression expression))
                    {
                        _context.GetLevel().Enqueue(expression);
                        break;
                    }
                }
                return VisitorAction.Skip;
            }
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (Operations.Peek() is FilterOperationField field)
            {
                for (var i = _context.FieldHandlers.Count - 1; i >= 0; i--)
                {
                    _context.FieldHandlers[i].Leave(
                        _context,
                        field,
                        node,
                        parent,
                        path,
                        ancestors);
                }
            }
            return base.Leave(node, parent, path, ancestors);
        }

        private bool TryCombineOperations(
            Queue<Expression> operations,
            Func<Expression, Expression, Expression> combine,
            out Expression combined)
        {
            if (operations.Count != 0)
            {
                combined = operations.Dequeue();

                while (operations.Count != 0)
                {
                    combined = combine(combined, operations.Dequeue());
                }

                return true;
            }

            combined = null;
            return false;
        }

        #endregion

        #region List

        public override VisitorAction Enter(
            ListValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _context.PushLevel(new Queue<Expression>());
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ListValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            var combine = Operations.Peek() is OrField
                ? new Func<Expression, Expression, Expression>(
                    (a, b) => Expression.OrElse(a, b))
                : new Func<Expression, Expression, Expression>(
                    (a, b) => Expression.AndAlso(a, b));

            Queue<Expression> operations = _context.PopLevel();

            if (TryCombineOperations(
                operations,
                combine,
                out Expression combined))
            {
                _context.GetLevel().Enqueue(combined);
            }

            return VisitorAction.Continue;
        }

        #endregion
    }
}

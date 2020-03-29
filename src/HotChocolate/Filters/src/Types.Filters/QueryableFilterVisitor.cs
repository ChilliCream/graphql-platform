using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
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

        public Expression CreateFilter()
        {
            return _context.GetClosure().CreateLambda();
        }

        #region Object Value

        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            ISyntaxVisitorContext context)
        {
            _context.PushLevel(new Queue<Expression>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            ISyntaxVisitorContext context)
        {
            Queue<Expression> operations = _context.PopLevel();

            if (TryCombineOperations(
                operations,
                (a, b) => Expression.AndAlso(a, b),
                out Expression combined))
            {
                _context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }

        #endregion

        #region Object Field

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxVisitorContext context)
        {
            base.Enter(node, context);

            if (Operations.Peek() is FilterOperationField field)
            {
                for (var i = _context.FieldHandlers.Count - 1; i >= 0; i--)
                {
                    if (_context.FieldHandlers[i].Enter(
                        field,
                        node,
                        _context,
                        out ISyntaxVisitorAction action))
                    {
                        return action;
                    }
                }
                for (var i = _context.OperationHandlers.Count - 1; i >= 0; i--)
                {
                    if (_context.OperationHandlers[i].TryHandle(
                        field.Operation,
                        field.Type,
                        node.Value,
                        _context,
                        out Expression expression))
                    {
                        _context.GetLevel().Enqueue(expression);
                        break;
                    }
                }
                return Skip;
            }
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxVisitorContext context)
        {
            if (Operations.Peek() is FilterOperationField field)
            {
                for (var i = _context.FieldHandlers.Count - 1; i >= 0; i--)
                {
                    _context.FieldHandlers[i].Leave(
                        field,
                        node,
                        _context);
                }
            }
            return base.Leave(node, context);
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

        protected override ISyntaxVisitorAction Enter(
            ListValueNode node,
            ISyntaxVisitorContext context)
        {
            _context.PushLevel(new Queue<Expression>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            ISyntaxVisitorContext context)
        {
            Func<Expression, Expression, Expression> combine =
                Operations.Peek() is OrField
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

            return Continue;
        }

        #endregion
    }
}

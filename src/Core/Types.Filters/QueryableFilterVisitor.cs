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
        private readonly IReadOnlyList<IExpressionOperationHandler> _opHandlers;
        private readonly IReadOnlyList<IExpressionFieldHandler> _fieldHandlers;
        private readonly ITypeConversion _converter;

        protected Stack<QueryableClosure> Closures { get; } = new Stack<QueryableClosure>();

        public QueryableFilterVisitor(
            InputObjectType initialType,
            Type source,
            ITypeConversion converter)
            : this(
                initialType, 
                source, 
                converter, 
                ExpressionOperationHandlers.All, 
                ExpressionFieldHandlers.All)
        {
        }

        public QueryableFilterVisitor(
            InputObjectType initialType,
            Type source,
            ITypeConversion converter,
            IEnumerable<IExpressionOperationHandler> operationHandlers,
            IEnumerable<IExpressionFieldHandler> fieldHandlers)
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

            _opHandlers = operationHandlers.ToArray();
            _fieldHandlers = fieldHandlers.ToArray();
            _converter = converter;
            Closures.Push(new QueryableClosure(source, "r"));
        }

        public Expression<Func<TSource, bool>> CreateFilter<TSource>()
        {
            return Closures.Peek().CreateLambda<Func<TSource, bool>>();
        }

        public Expression<Func<TSource, bool>> CreateFilterInMemory<TSource>()
        {
            return Closures.Peek().CreateLambdaWithNullCheck<Func<TSource, bool>>();
        }

        #region Object Value
        public override VisitorAction Enter(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Closures.Peek().Level.Push(new Queue<Expression>());
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Queue<Expression> operations = Closures.Peek().Level.Pop();

            if (TryCombineOperations(
                operations,
                (a, b) => Expression.AndAlso(a, b),
                out Expression combined))
            {
                Closures.Peek().Level.Peek().Enqueue(combined);
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
                for (var i = _fieldHandlers.Count - 1; i >= 0; i--)
                {
                    if (_fieldHandlers[i].Enter(
                        field,
                        node,
                        parent,
                        path,
                        ancestors,
                        Closures,
                        out VisitorAction action))
                    {
                        return action;
                    }
                }
                for (var i = _opHandlers.Count - 1; i >= 0; i--)
                {
                    if (_opHandlers[i].TryHandle(
                        field.Operation,
                        field.Type,
                        node.Value,
                        Closures.Peek().Instance.Peek(),
                        _converter,
                        out Expression expression))
                    {
                        Closures.Peek().Level.Peek().Enqueue(expression);
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
                for (var i = _fieldHandlers.Count - 1; i >= 0; i--)
                {
                    _fieldHandlers[i].Leave(
                        field,
                        node,
                        parent,
                        path,
                        ancestors,
                        Closures);
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
            Closures.Peek().Level.Push(new Queue<Expression>());
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

            Queue<Expression> operations = Closures.Peek().Level.Pop();

            if (TryCombineOperations(
                operations,
                combine,
                out Expression combined))
            {
                Closures.Peek().Level.Peek().Enqueue(combined);
            }

            return VisitorAction.Continue;
        }

        #endregion
    }
}

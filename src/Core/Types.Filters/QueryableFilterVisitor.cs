using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitor
        : FilterVisitorBase
    {
        private readonly Type _source;
        private readonly IReadOnlyList<IExpressionOperationHandler> _opHandlers;
        private readonly Expression _instance;
        private readonly ParameterExpression _parameter;

        public QueryableFilterVisitor(
            InputObjectType initialType,
            Type source)
            : base(initialType)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _instance = _parameter = Expression.Parameter(source);
            _opHandlers = ExpressionOperationHandlers.All;

            Level.Push(new Queue<Expression>());
            Instance.Push(_instance);
        }

        public QueryableFilterVisitor(
            InputObjectType initialType,
            Type source,
            IEnumerable<IExpressionOperationHandler> operationHandlers)
            : base(initialType)
        {
            if (operationHandlers is null)
            {
                throw new ArgumentNullException(nameof(operationHandlers));
            }

            _source = source ?? throw new ArgumentNullException(nameof(source));
            _opHandlers = operationHandlers.ToArray();
            _instance = _parameter = Expression.Parameter(source);

            Level.Push(new Queue<Expression>());
            Instance.Push(_instance);
        }

        // TODO : get rid of the generic type parameter
        public Expression<Func<TSource, bool>> CreateFilter<TSource>()
        {
            return Expression.Lambda<Func<TSource, bool>>(
                Level.Peek().Peek(),
                _parameter);
        }

        protected Stack<Queue<Expression>> Level { get; } =
            new Stack<Queue<Expression>>();

        protected Stack<Expression> Instance { get; } =
            new Stack<Expression>();

        #region Object Value

        public override VisitorAction Enter(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Level.Push(new Queue<Expression>());
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Queue<Expression> operations = Level.Pop();

            if (TryCombineOperations(
                operations,
                (a, b) => Expression.AndAlso(a, b),
                out Expression combined))
            {
                Level.Peek().Enqueue(combined);
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
                // TODO : needed only if we allow objects
                // Instance.Push(Expression.Property(
                //     Instance.Peek(),
                //     field.Operation.Property));

                for (int i = _opHandlers.Count - 1; i >= 0; i--)
                {
                    if (_opHandlers[i].TryHandle(
                        field.Operation,
                        node.Value,
                        Instance.Peek(),
                        out Expression expression))
                    {
                        Level.Peek().Enqueue(expression);
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
            // TODO : needed only if we allow objects
            // if (Operations.Peek() is FilterOperationField)
            // {
            //     Instance.Pop();
            // }
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
            switch (Operations.Peek())
            {
                case AndField and:
                case OrField or:
                    Level.Push(new Queue<Expression>());
                    break;
            }

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
                    (a, b) => Expression.AndAlso(a, b))
                : new Func<Expression, Expression, Expression>(
                    (a, b) => Expression.OrElse(a, b));

            Queue<Expression> operations = Level.Pop();

            if (TryCombineOperations(
                operations,
                combine,
                out Expression combined))
            {
                Level.Peek().Enqueue(combined);
            }

            return VisitorAction.Continue;
        }

        #endregion
    }


}

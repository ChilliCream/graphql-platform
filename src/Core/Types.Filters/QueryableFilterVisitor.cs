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
        private const string _parameterName = "t";
        private readonly IReadOnlyList<IExpressionOperationHandler> _opHandlers;
        private readonly ParameterExpression _parameter;
        private readonly ITypeConversion _converter;

        public QueryableFilterVisitor(
            InputObjectType initialType,
            Type source,
            ITypeConversion converter)
            : base(initialType)
        {
            _parameter = Expression.Parameter(source, _parameterName);
            _opHandlers = ExpressionOperationHandlers.All;
            _converter = converter;

            Level.Push(new Queue<Expression>());
            Instance.Push(_parameter);
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

            _opHandlers = operationHandlers.ToArray();
            _parameter = Expression.Parameter(source, _parameterName);

            Level.Push(new Queue<Expression>());
            Instance.Push(_parameter);
        }

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
                        field.Type,
                        node.Value,
                        Instance.Peek(),
                        _converter,
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
            Level.Push(new Queue<Expression>());
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

        #region Scalar Values

        public override VisitorAction Enter(
            StringValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            StringValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
            IntValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            IntValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
            FloatValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            FloatValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
            EnumValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            EnumValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
            BooleanValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            BooleanValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class QueryableFilterVisitor
        : FilterVisitorBase<QueryableFilterVisitorContext>
    {
        protected QueryableFilterVisitor()
        {

        }

        #region Object Value

        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            QueryableFilterVisitorContext context)
        {
            context.PushLevel(new Queue<Expression>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            QueryableFilterVisitorContext context)
        {
            Queue<Expression> operations = context.PopLevel();

            if (TryCombineOperations(
                operations,
                (a, b) => Expression.AndAlso(a, b),
                out Expression? combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }

        #endregion

        #region Object Field

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            QueryableFilterVisitorContext context)
        {
            base.Enter(node, context);

            if (context.Operations.Peek() is FilterOperationField field)
            {
                for (var i = context.FieldHandlers.Count - 1; i >= 0; i--)
                {
                    if (context.FieldHandlers[i].Enter(
                        field,
                        node,
                        context,
                        out ISyntaxVisitorAction action))
                    {
                        return action;
                    }
                }
                for (var i = context.OperationHandlers.Count - 1; i >= 0; i--)
                {
                    if (context.OperationHandlers[i].TryHandle(
                        field.Operation,
                        field.Type,
                        node.Value,
                        context,
                        out Expression? expression))
                    {
                        context.GetLevel().Enqueue(expression);
                        break;
                    }
                }
                return SkipAndLeave;
            }
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            QueryableFilterVisitorContext context)
        {
            if (context.Operations.Peek() is FilterOperationField field)
            {
                for (var i = context.FieldHandlers.Count - 1; i >= 0; i--)
                {
                    context.FieldHandlers[i].Leave(
                        field,
                        node,
                        context);
                }
            }
            return base.Leave(node, context);
        }

        private bool TryCombineOperations(
            Queue<Expression> operations,
            Func<Expression, Expression, Expression> combine,
            [NotNullWhen(true)] out Expression? combined)
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
            QueryableFilterVisitorContext context)
        {
            context.PushLevel(new Queue<Expression>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            QueryableFilterVisitorContext context)
        {
            Func<Expression, Expression, Expression> combine =
                context.Operations.Peek() is OrField
                    ? new Func<Expression, Expression, Expression>(
                        (a, b) => Expression.OrElse(a, b))
                    : new Func<Expression, Expression, Expression>(
                        (a, b) => Expression.AndAlso(a, b));

            Queue<Expression> operations = context.PopLevel();

            if (TryCombineOperations(
                operations,
                combine,
                out Expression? combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }

        #endregion

        public static QueryableFilterVisitor Default = new QueryableFilterVisitor();
    }
}

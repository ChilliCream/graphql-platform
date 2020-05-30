using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class ObjectFieldHandler
    {
        public static bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<Expression> context,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                if (field.Operation.IsNullable)
                {
                    MemberExpression nestedProperty = Expression.Property(
                        context.GetInstance(),
                        field.Operation.Property);

                    Expression expression =
                        FilterExpressionBuilder.Equals(nestedProperty, null!);

                    context.GetLevel().Enqueue(expression);
                }
                else
                {
                    context.ReportError(
                        ErrorHelper.CreateNonNullError(field, node, context));
                }

                action = SyntaxVisitor.Skip;
                return true;
            }

            if (FilterOperationKind.Object.Equals(field.Operation.Kind))
            {
                MemberExpression nestedProperty = Expression.Property(
                    context.GetInstance(),
                    field.Operation.Property);

                context.PushInstance(nestedProperty);
                action = SyntaxVisitor.Continue;
                return true;
            }
            action = null;
            return false;
        }

        public static void Leave(
            FilterOperationField field,
            ObjectFieldNode _,
            IFilterVisitorContext<Expression> context)
        {
            if (context is QueryableFilterVisitorContext queryableContext)
            {
                if (FilterOperationKind.Object.Equals(field.Operation.Kind))
                {
                    // Deque last expression to prefix with nullcheck
                    Expression condition = context.GetLevel().Dequeue();
                    Expression property = context.GetInstance();

                    // wrap last expression only if  in memory
                    if (queryableContext.InMemory)
                    {
                        condition = FilterExpressionBuilder.NotNullAndAlso(
                            property, condition);
                    }

                    context.GetLevel().Enqueue(condition);
                    context.PopInstance();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}

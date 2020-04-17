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
            IQueryableFilterVisitorContext context,
            out ISyntaxVisitorAction action)
        {
            if (node.Value.IsNull())
            {
                if (field.Operation.IsNullable)
                {
                    MemberExpression nestedProperty = Expression.Property(
                        context.GetInstance(),
                        field.Operation.Property);

                    Expression expression
                        = FilterExpressionBuilder.Equals(nestedProperty, null!);

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

            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                MemberExpression nestedProperty = Expression.Property(
                    context.GetInstance(),
                    field.Operation.Property);

                context.PushInstance(nestedProperty);
                action = SyntaxVisitor.Continue;
                return true;
            }
            action = SyntaxVisitor.SkipAndLeave;
            return false;
        }

        public static void Leave(
            FilterOperationField field,
            ObjectFieldNode node,
            IQueryableFilterVisitorContext context)
        {
            if (field.Operation.Kind == FilterOperationKind.Object)
            {
                // Deque last expression to prefix with nullcheck
                Expression condition = context.GetLevel().Dequeue();
                Expression property = context.GetInstance();

                // wrap last expression only if  in memory
                if (context.InMemory)
                {
                    condition = FilterExpressionBuilder.NotNullAndAlso(
                        property, condition);
                }

                context.GetLevel().Enqueue(condition);
                context.PopInstance();
            }
        }
    }
}

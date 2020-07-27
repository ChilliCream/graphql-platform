using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableDefaultFieldHandler
        : FilterFieldHandler<Expression, QueryableFilterContext>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) =>
            !(fieldDefinition is FilterOperationFieldDefinition) &&
                fieldDefinition.Member is PropertyInfo;

        public override bool TryHandleEnter(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {

            if (node.Value.IsNull())
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, node.Value, context));

                action = SyntaxVisitor.Skip;
                return true;
            }

            if (field is { } &&
                field.Member is PropertyInfo propertyInfo)
            {
                MemberExpression nestedProperty = Expression.Property(
                    context.GetInstance(),
                    propertyInfo);

                context.PushInstance(nestedProperty);
                action = SyntaxVisitor.Continue;
                return true;
            }

            action = null;
            return false;
        }

        public override bool TryHandleLeave(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field is { } &&
                field.Member is PropertyInfo)
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
                action = SyntaxVisitor.Continue;
                return true;
            }

            action = null;
            return false;
        }
    }
}
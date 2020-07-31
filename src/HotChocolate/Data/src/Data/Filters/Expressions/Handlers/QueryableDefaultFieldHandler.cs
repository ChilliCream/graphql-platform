using System;
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

            if (field is { })
            {
                Expression nestedProperty;
                if (field.Member is PropertyInfo propertyInfo)
                {
                    nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
                }
                else if (field.Member is MethodInfo methodInfo)
                {
                    nestedProperty = Expression.Property(context.GetInstance(), methodInfo);
                }
                else
                {
                    throw new InvalidOperationException();
                }

                context.PushInstance(nestedProperty);
                context.ClrTypes.Push(nestedProperty.Type);
                context.TypeInfos.Push(field.TypeInfo);
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
            if (field is { })
            {
                // Deque last
                Expression condition = context.GetLevel().Dequeue();

                context.PopInstance();
                context.ClrTypes.Pop();
                context.TypeInfos.Pop();

                if (context.InMemory)
                {
                    condition = FilterExpressionBuilder.NotNullAndAlso(
                        context.GetInstance(), condition);
                }

                context.GetLevel().Enqueue(condition);
                action = SyntaxVisitor.Continue;
                return true;
            }

            action = null;
            return false;
        }
    }
}
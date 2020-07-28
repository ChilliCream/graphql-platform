using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public abstract class QueryableListOperationHandlerBase
            : FilterFieldHandler<Expression, QueryableFilterContext>
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Operation == Operation;
        }

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

            if (context.TryGetDeclaringField(out IFilterField? parentField))
            {
                Expression nestedProperty = context.GetInstance();
                context.PushInstance(nestedProperty);

                Type closureType = GetTypeFor(parentField);
                context.ClrTypes.Push(closureType);
                context.AddScope();

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
            FilterScope<Expression> nestedScope = context.PopScope();

            Type closureType = context.ClrTypes.Pop();

            if (nestedScope is QueryableScope nestedClosure &&
                nestedClosure.TryCreateLambda(out LambdaExpression? lambda))
            {
                Expression expression = HandleListOperation(
                    context,
                    declaringType,
                    field,
                    fieldType,
                    node,
                    closureType,
                    lambda);

                if (context.InMemory)
                {
                    expression = FilterExpressionBuilder.NotNullAndAlso(
                        context.GetInstance(), expression);
                }
                context.GetLevel().Enqueue(expression);
            }
            context.PopInstance();

            action = SyntaxVisitor.Continue;
            return true;
        }

        protected abstract Expression HandleListOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node,
            Type closureType,
            LambdaExpression lambda);

        private static Type GetTypeFor(IFilterField field)
        {
            if (field.ElementType is Type baseType)
            {
                return baseType;
            }
            return field.RuntimeType;
        }
    }
}

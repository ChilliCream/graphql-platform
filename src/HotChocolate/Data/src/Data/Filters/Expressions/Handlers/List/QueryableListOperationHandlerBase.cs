using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

            if (context.TypeInfos.TryPeek(out FilterTypeInfo? filterTypeInfo) &&
                filterTypeInfo.TypeArguments.FirstOrDefault() is FilterTypeInfo element)
            {

                Expression nestedProperty = context.GetInstance();
                context.PushInstance(nestedProperty);

                Type closureType = element.Type;
                context.TypeInfos.Push(element);
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
            context.ClrTypes.Pop();
            FilterTypeInfo typeInfo = context.TypeInfos.Pop();

            if (context.TryCreateLambda(out LambdaExpression? lambda))
            {
                context.Scopes.Pop();
                Expression expression = HandleListOperation(
                    context,
                    declaringType,
                    field,
                    fieldType,
                    node,
                    typeInfo.Type,
                    lambda);

                if (context.InMemory)
                {
                    expression = FilterExpressionBuilder.NotNullAndAlso(
                        context.GetInstance(), expression);
                }
                context.GetLevel().Enqueue(expression);
            }


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
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters.Expressions
{
    /// <summary>
    /// The base of a operation handler specific for <see cref="IListFilterInputType"/>
    /// If the <see cref="FilterTypeInterceptor"/> encounters a operation field that implements
    /// <see cref="IListFilterInputType"/> and matches the operation identifier
    /// defined in <see cref="QueryableComparableOperationHandler.Operation"/> the handler is bound
    /// to the field
    /// </summary>
    public abstract class QueryableListOperationHandlerBase
        : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        /// <summary>
        /// Specifies the identifier of the operations that should be handled by this handler
        /// </summary>
        protected abstract int Operation { get; }

        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }

        /// <inheritdoc />
        public override bool TryHandleEnter(
            QueryableFilterContext context,
            IFilterField field,
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

            if (context.RuntimeTypes.Count > 0 &&
                context.RuntimeTypes.Peek().TypeArguments is { Count: > 0 } args)
            {
                Expression nestedProperty = context.GetInstance();
                context.PushInstance(nestedProperty);

                IExtendedType element = args[0];
                context.RuntimeTypes.Push(element);
                context.AddScope();

                action = SyntaxVisitor.Continue;
                return true;
            }

            action = null;
            return false;
        }

        /// <inheritdoc />
        public override bool TryHandleLeave(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            IExtendedType runtimeType = context.RuntimeTypes.Pop();

            if (context.TryCreateLambda(out LambdaExpression? lambda))
            {
                context.Scopes.Pop();
                Expression instance = context.PopInstance();
                Expression expression = HandleListOperation(
                    context,
                    field,
                    node,
                    runtimeType.Source,
                    lambda);

                if (context.InMemory)
                {
                    expression = FilterExpressionBuilder.NotNullAndAlso(
                        instance,
                        expression);
                }

                context.GetLevel().Enqueue(expression);
            }


            action = SyntaxVisitor.Continue;
            return true;
        }

        /// <summary>
        /// Maps a operation field to a list filter definition.
        /// This method is called when the <see cref="FilterVisitor{TContext,T}"/> enters a
        /// field
        /// </summary>
        /// <param name="context">The context of the visitor</param>
        /// <param name="field">The currently visited filter field</param>
        /// <param name="closureType">The runtime type of the scope</param>
        /// <param name="lambda">The expression of the nested operations</param>
        /// <returns></returns>
        protected abstract Expression HandleListOperation(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            LambdaExpression lambda);
    }
}

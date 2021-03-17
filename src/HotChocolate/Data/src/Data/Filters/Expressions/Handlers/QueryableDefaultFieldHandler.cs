using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters.Expressions
{
    /// <summary>
    /// The default handler for all <see cref="FilterField"/> for the
    /// <see cref="QueryableFilterProvider"/>
    /// </summary>
    public class QueryableDefaultFieldHandler
        : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        /// <summary>
        /// Checks if the field not a filter operations field and if the member is defined on this
        /// field
        /// </summary>
        /// <param name="context">The current context</param>
        /// <param name="typeDefinition">The definition of the type that declares the field</param>
        /// <param name="fieldDefinition">The definition of the field</param>
        /// <returns>True in case the field can be handled</returns>
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition) =>
            !(fieldDefinition is FilterOperationFieldDefinition) &&
            fieldDefinition.Member is not null;

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

            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            Expression nestedProperty;
            if (field.Member is PropertyInfo propertyInfo)
            {
                nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
            }
            else if (field.Member is MethodInfo methodInfo)
            {
                nestedProperty = Expression.Call(context.GetInstance(), methodInfo);
            }
            else
            {
                throw new InvalidOperationException();
            }

            context.PushInstance(nestedProperty);
            context.RuntimeTypes.Push(field.RuntimeType);
            action = SyntaxVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            // Deque last
            Expression condition = context.GetLevel().Dequeue();

            context.PopInstance();
            context.RuntimeTypes.Pop();

            if (context.InMemory)
            {
                condition = FilterExpressionBuilder.NotNullAndAlso(
                    context.GetInstance(),
                    condition);
            }

            context.GetLevel().Enqueue(condition);
            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}

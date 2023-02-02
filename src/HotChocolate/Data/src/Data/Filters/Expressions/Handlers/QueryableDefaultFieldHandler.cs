using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters.Expressions;

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
        fieldDefinition is not FilterOperationFieldDefinition &&
        (fieldDefinition.Member is not null || fieldDefinition.Expression is not null);

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
        if (field.Metadata is ExpressionFilterMetadata { Expression: LambdaExpression expression })
        {
            if (expression.Parameters.Count != 1 ||
                expression.Parameters[0].Type != context.RuntimeTypes.Peek()!.Source)
            {
                throw ThrowHelper.QueryableFiltering_ExpressionParameterInvalid(
                    field.RuntimeType.Source,
                    field);
            }

            nestedProperty = ReplaceVariableExpressionVisitor
                .ReplaceParameter(expression, expression.Parameters[0], context.GetInstance())
                .Body;
        }
        else
        {
            nestedProperty = field.Member switch
            {
                PropertyInfo propertyInfo =>
                    Expression.Property(context.GetInstance(), propertyInfo),

                MethodInfo methodInfo =>
                    Expression.Call(context.GetInstance(), methodInfo),

                null =>
                    throw ThrowHelper.QueryableFiltering_NoMemberDeclared(field),

                _ =>
                    throw ThrowHelper.QueryableFiltering_MemberInvalid(field.Member, field)
            };
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
        var condition = context.GetLevel().Dequeue();

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

    private sealed class ReplaceVariableExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _replacement;
        private readonly ParameterExpression _parameter;

        public ReplaceVariableExpressionVisitor(
            Expression replacement,
            ParameterExpression parameter)
        {
            _replacement = replacement;
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _parameter)
            {
                return _replacement;
            }
            return base.VisitParameter(node);
        }

        public static LambdaExpression ReplaceParameter(
            LambdaExpression lambda,
            ParameterExpression parameter,
            Expression replacement)
            => (LambdaExpression)
                new ReplaceVariableExpressionVisitor(replacement, parameter).Visit(lambda);
    }
}

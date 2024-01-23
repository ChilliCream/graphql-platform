using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting.Expressions;

public class QueryableDefaultSortFieldHandler
    : SortFieldHandler<QueryableSortContext, QueryableSortOperation>
{
    public override bool CanHandle(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition) =>
        fieldDefinition.Member is not null || fieldDefinition.Expression is not null;

    public override bool TryHandleEnter(
        QueryableSortContext context,
        ISortField field,
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

        if (!(context.GetInstance() is QueryableFieldSelector lastFieldSelector))
        {
            throw ThrowHelper.Sorting_InvalidState_ParentIsNoFieldSelector(field);
        }

        var lastSelector = lastFieldSelector.Selector;
        Expression nextSelector;
        if (field.Metadata is ExpressionSortMetadata { Expression: LambdaExpression expression, })
        {
            if (expression.Parameters.Count != 1 ||
                expression.Parameters[0].Type != context.RuntimeTypes.Peek()!.Source)
            {
                throw ThrowHelper.QueryableSorting_ExpressionParameterInvalid(
                    field.RuntimeType.Source,
                    field);
            }

            nextSelector = ReplaceVariableExpressionVisitor
                .ReplaceParameter(expression, expression.Parameters[0], lastSelector)
                .Body;
        }
        else
        {
            nextSelector = field.Member switch
            {
                PropertyInfo i => Expression.Property(lastSelector, i),
                MethodInfo i => Expression.Call(lastSelector, i),
                { } i => throw ThrowHelper.QueryableSorting_MemberInvalid(i, field),
                null => throw ThrowHelper.QueryableSorting_NoMemberDeclared(field),
            };
        }

        if (context.InMemory)
        {
            nextSelector = SortExpressionBuilder.IfNullThenDefault(
                lastSelector,
                nextSelector,
                Expression.Default(field.RuntimeType.Source));
        }

        context.PushInstance(lastFieldSelector.WithSelector(nextSelector));
        context.RuntimeTypes.Push(field.RuntimeType);
        action = SyntaxVisitor.Continue;
        return true;
    }

    public override bool TryHandleLeave(
        QueryableSortContext context,
        ISortField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        if (field.RuntimeType is null)
        {
            action = null;
            return false;
        }

        // Deque last
        context.PopInstance();
        context.RuntimeTypes.Pop();

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

        protected override Expression VisitExtension(Expression node) => node.CanReduce ? base.VisitExtension(node) : node;

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

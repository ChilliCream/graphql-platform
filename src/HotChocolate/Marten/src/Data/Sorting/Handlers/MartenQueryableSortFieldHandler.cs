using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Marten.Sorting.Handlers;

public class MartenQueryableSortFieldHandler: QueryableDefaultSortFieldHandler
{
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
        if (field.Metadata is ExpressionSortMetadata { Expression: LambdaExpression expression })
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

        context.PushInstance(lastFieldSelector.WithSelector(nextSelector));
        context.RuntimeTypes.Push(field.RuntimeType);
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

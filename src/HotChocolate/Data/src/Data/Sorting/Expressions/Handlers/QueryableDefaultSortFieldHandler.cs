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
        fieldDefinition.Member is not null;

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

        Expression lastSelector = lastFieldSelector.Selector;


        Expression nextSelector = field.Member switch
        {
            PropertyInfo i => Expression.Property(lastSelector, i),
            MethodInfo i => Expression.Call(lastSelector, i),
            { } i => throw ThrowHelper.QueryableSorting_MemberInvalid(i, field),
            null => throw ThrowHelper.QueryableSorting_NoMemberDeclared(field),
        };

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
}

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using static HotChocolate.Data.Projections.Expressions.ProjectionExpressionBuilder;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionFieldHandler
    : QueryableProjectionHandlerBase
{
    public override bool CanHandle(Selection selection)
        => selection.Field.Member is not null && !selection.IsLeaf;

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;
        Expression nestedProperty;
        Type memberType;

        if (field.Member is PropertyInfo { CanWrite: true } propertyInfo)
        {
            if (QueryableProjectionJsonbDetector.IsJsonbMappedProperty(context, propertyInfo))
            {
                action = SelectionVisitor.SkipAndLeave;

                return true;
            }

            memberType = propertyInfo.PropertyType;
            nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);
        }
        else
        {
            action = SelectionVisitor.Skip;

            return true;
        }

        // We add a new scope for the sub selection. This allows a new member initialization
        context.AddScope(memberType);

        // We push the instance onto the new scope. We do not need this instance on the current
        // scope.
        context.PushInstance(nestedProperty);

        action = SelectionVisitor.Continue;

        return true;
    }

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;

        if (field.Member is not PropertyInfo propertyInfo)
        {
            action = null;

            return false;
        }

        if (QueryableProjectionJsonbDetector.IsJsonbMappedProperty(context, propertyInfo))
        {
            if (context.Scopes.Count > 0
                && context.Scopes.Peek() is QueryableProjectionScope closure)
            {
                var instance = closure.Instance.Peek();

                closure.Level
                    .Peek()
                    .Enqueue(Expression.Bind(propertyInfo, Expression.Property(instance, propertyInfo)));

                action = SelectionVisitor.Continue;

                return true;
            }

            action = SelectionVisitor.Skip;

            return true;
        }

        // Dequeue last
        var scope = context.PopScope();

        if (scope is not QueryableProjectionScope queryableScope)
        {
            action = null;

            return false;
        }

        if (!context.TryGetQueryableScope(out var parentScope))
        {
            throw ThrowHelper.ProjectionVisitor_InvalidState_NoParentScope();
        }

        Expression nestedProperty;
        nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);

        // If the nested scope has no projectable members we keep the original value.
        // This happens for members like JsonDocument where selected subfields are read-only.
        if (!queryableScope.HasAbstractTypes() && queryableScope.Level.Peek().Count == 0)
        {
            parentScope.Level
                .Peek()
                .Enqueue(Expression.Bind(propertyInfo, nestedProperty));

            action = SelectionVisitor.Continue;

            return true;
        }

        var memberInit = queryableScope.CreateMemberInit();

        if (context.InMemory)
        {
            parentScope.Level
                .Peek()
                .Enqueue(Expression.Bind(propertyInfo, NotNullAndAlso(nestedProperty, memberInit)));
        }
        else
        {
            parentScope.Level
                .Peek()
                .Enqueue(Expression.Bind(propertyInfo, memberInit));
        }

        action = SelectionVisitor.Continue;

        return true;
    }

    public static QueryableProjectionFieldHandler Create(ProjectionProviderContext context) => new();
}

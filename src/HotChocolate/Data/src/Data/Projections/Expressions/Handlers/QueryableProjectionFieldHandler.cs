using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using static HotChocolate.Data.Projections.Expressions.ProjectionExpressionBuilder;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionFieldHandler
    : QueryableProjectionHandlerBase
{
    private static readonly NullabilityInfoContext s_nullability = new();

    public override bool CanHandle(Selection selection)
        => !selection.IsLeaf && CanProjectMember(selection);

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

        if (field.Member is null)
        {
            action = null;

            return false;
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

        if (field.Member is not PropertyInfo propertyInfo)
        {
            action = SelectionVisitor.Skip;

            return true;
        }

        var nestedProperty = Expression.Property(context.GetInstance(), propertyInfo);

        // If the nested scope has no projectable members we keep the original value.
        // This happens for members like JsonDocument where selected subfields are read-only.
        if (!queryableScope.HasAbstractTypes() && queryableScope.Level.Peek().Count == 0)
        {
            parentScope.Level
                .Peek()
                .Enqueue(Expression.Bind(field.Member, nestedProperty));

            action = SelectionVisitor.Continue;

            return true;
        }

        var memberInit = queryableScope.CreateMemberInit();

        if (context.InMemory && ShouldApplyNullGuard(propertyInfo))
        {
            parentScope.Level
                .Peek()
                .Enqueue(Expression.Bind(field.Member, NotNullAndAlso(nestedProperty, memberInit)));
        }
        else
        {
            parentScope.Level
                .Peek()
                .Enqueue(Expression.Bind(field.Member, memberInit));
        }

        action = SelectionVisitor.Continue;

        return true;
    }

    private static bool ShouldApplyNullGuard(PropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            return false;
        }

        // Preserve the existing null-guard behavior for entity-like references. For
        // value-object/complex-like references (non-null and no identity member), the guard
        // generates unsupported complex-type null comparisons on EF Core 8/9.
        if (s_nullability.Create(property).ReadState is not NullabilityState.NotNull)
        {
            return true;
        }

        return HasIdentityMember(property.PropertyType);
    }

    private static bool HasIdentityMember(Type type)
        => type.GetProperty(
                "Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            is not null;

    public static QueryableProjectionFieldHandler Create(ProjectionProviderContext context) => new();
}

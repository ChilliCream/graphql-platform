using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public abstract class QueryableProjectionHandlerBase
    : ProjectionFieldHandler<QueryableProjectionContext>
{
    protected static bool CanProjectMember(Selection selection)
    {
        if (selection.Field.Member is null)
        {
            return false;
        }

        // Explicit opt-in should always project regardless of resolver source.
        if (selection.Field.IsAlwaysProjected())
        {
            return true;
        }

        var resolverMember = selection.Field.ResolverMember;

        if (resolverMember is null)
        {
            return true;
        }

        if (resolverMember.ReflectedType == selection.Field.DeclaringType.RuntimeType)
        {
            return true;
        }

        // When a member is explicitly bound we keep projecting it.
        return resolverMember.IsDefined(typeof(BindMemberAttribute), inherit: true);
    }

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        action = SelectionVisitor.Continue;
        return true;
    }

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        action = SelectionVisitor.Continue;
        return true;
    }
}

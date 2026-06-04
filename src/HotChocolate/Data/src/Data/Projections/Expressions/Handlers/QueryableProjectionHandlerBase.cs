using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

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

        // Fields inherited from an interface carry the interface member as their resolver
        // member; the runtime type implements that interface, so the member can be projected.
        if (resolverMember.ReflectedType?.IsAssignableFrom(
            selection.Field.DeclaringType.RuntimeType) == true)
        {
            return true;
        }

        // Explicit member replacements (e.g. fluent ResolveWith on a shadowed property)
        // must keep projecting the underlying member so custom resolvers
        // can access the shadowed data on projected parents.
        if (selection.Field.Flags.HasFlag(CoreFieldFlags.MemberReplacement))
        {
            return true;
        }

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

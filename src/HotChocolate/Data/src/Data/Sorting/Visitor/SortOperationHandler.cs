using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a handler that can be bound to a <see cref="SortField"/>. The handler is
/// executed during the visitation of an input object.
/// </summary>
public abstract class SortOperationHandler<TContext, T>
    : ISortOperationHandler<TContext, T>
    where TContext : SortVisitorContext<T>
{
    /// <inheritdoc/>
    public virtual bool TryHandleEnter(
        TContext context,
        ISortField field,
        SortEnumValue? sortValue,
        EnumValueNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        action = null;
        return false;
    }

    /// <inheritdoc />
    public abstract bool CanHandle(
        ITypeCompletionContext context,
        EnumTypeConfiguration typeDefinition,
        SortEnumValueConfiguration valueConfiguration);
}

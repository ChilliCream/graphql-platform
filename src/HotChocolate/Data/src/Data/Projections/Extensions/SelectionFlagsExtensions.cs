using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Projections;

/// <summary>
/// Provides extension methods in combination with the <see cref="SelectionFlags"/>
/// </summary>
public static class SelectionFlagsExtensions
{
    /// <summary>
    /// Specified if the field has a first or default middleware.
    /// </summary>
    /// <param name="selection">The selection.</param>
    /// <returns>
    /// <c>true</c> if the field has a first or default middleware; otherwise, <c>false</c>.</returns>
    public static bool IsFirstOrDefault(this Selection selection)
    {
        var flags = selection.Field.Features.Get<SelectionFlags?>() ?? SelectionFlags.None;
        return (flags & SelectionFlags.FirstOrDefault) == SelectionFlags.FirstOrDefault;
    }

    /// <summary>
    /// Specifies if the field has a single or default middleware.
    /// </summary>
    /// <param name="selection">The selection.</param>
    /// <returns>
    /// <c>true</c> if the field has a single or default middleware; otherwise, <c>false</c>.</returns>
    public static bool IsSingleOrDefault(this Selection selection)
    {
        var flags = selection.Field.Features.Get<SelectionFlags?>() ?? SelectionFlags.None;
        return (flags & SelectionFlags.SingleOrDefault) == SelectionFlags.SingleOrDefault;
    }

    /// <summary>
    /// Specifies if the field is a list.
    /// </summary>
    /// <param name="selection">The selection.</param>
    /// <returns>
    /// <c>true</c> if the field is a list; otherwise, <c>false</c>.</returns>
    public static bool IsMemberIsList(this Selection selection)
    {
        var flags = selection.Field.Features.Get<SelectionFlags?>() ?? SelectionFlags.None;
        return (flags & SelectionFlags.MemberIsList) == SelectionFlags.MemberIsList;
    }

    /// <summary>
    /// Specifies if the specified <paramref name="flags"/> are set on the field.
    /// </summary>
    /// <param name="selection">The selection.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="flags"/> are set on the field; otherwise, <c>false</c>.</returns>
    public static bool IsSelectionFlags(this Selection selection, SelectionFlags flags)
    {
        var actualFlags = selection.Field.Features.Get<SelectionFlags?>() ?? SelectionFlags.None;
        return (actualFlags & flags) == flags;
    }

    /// <summary>
    /// Specifies if the specified <paramref name="flags"/> are set on the field.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="flags"/> are set on the field; otherwise, <c>false</c>.</returns>
    public static bool IsSelectionFlags(this IFieldDefinition field, SelectionFlags flags)
    {
        ArgumentNullException.ThrowIfNull(field);

        var actualFlags = field.Features.Get<SelectionFlags?>() ?? SelectionFlags.None;
        return (actualFlags & flags) == flags;
    }

    /// <summary>
    /// Updates the selection flags.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>
    /// <c>true</c> if the selection flags were updated; otherwise, <c>false</c>.</returns>
    public static bool AddSelectionFlags(this ObjectFieldConfiguration field, SelectionFlags flags)
    {
        ArgumentNullException.ThrowIfNull(field);

        var actualFlags = field.Features.Get<SelectionFlags?>() ?? SelectionFlags.None;
        actualFlags |= flags;
        field.Features.Set<SelectionFlags?>(actualFlags);
        return true;
    }
}

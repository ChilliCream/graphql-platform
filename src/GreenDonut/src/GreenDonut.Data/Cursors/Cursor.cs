using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace GreenDonut.Data.Cursors;

/// <summary>
/// Represents a pagination cursor used to indicate a specific position within a dataset.
/// </summary>
/// <param name="Values">
/// The cursor values corresponding to the fields used for pagination, typically keys.
/// </param>
/// <param name="Offset">
/// An optional offset to skip additional items from the current cursor position.
/// Positive values skip forward, negative values skip backward.
/// </param>
/// <param name="PageIndex">
/// The zero-based index of the current page within the paginated dataset.
/// </param>
/// <param name="TotalCount">
/// The total number of items in the dataset, if known. Can be <c>null</c> if not available.
/// </param>
public record Cursor(
    ImmutableArray<object?> Values,
    int? Offset = null,
    int? PageIndex = null,
    int? TotalCount = null)
{
    [MemberNotNullWhen(true, nameof(Offset), nameof(PageIndex), nameof(TotalCount))]
    public bool IsRelative => Offset.HasValue && PageIndex.HasValue && TotalCount.HasValue;
}

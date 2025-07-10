#nullable enable

using System.Collections.Immutable;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The paging options.
/// </summary>
public class PagingOptions
{
    /// <summary>
    /// Gets or sets the default page size.
    /// </summary>
    public int? DefaultPageSize { get; set; }

    /// <summary>
    /// Gets or sets the max allowed page size.
    /// </summary>
    public int? MaxPageSize { get; set; }

    /// <summary>
    /// Defines whether a <c>totalCount</c> field shall be
    /// exposed on the Connection type, returning the total
    /// count of items in the paginated data set.
    /// </summary>
    public bool? IncludeTotalCount { get; set; }

    /// <summary>
    /// Defines if backward pagination is allowed or deactivated
    /// </summary>
    public bool? AllowBackwardPagination { get; set; }

    /// <summary>
    /// Defines if the paging middleware shall require the
    /// API consumer to specify paging boundaries.
    /// </summary>
    public bool? RequirePagingBoundaries { get; set; }

    /// <summary>
    /// Connection names are by default inferred from the field name to
    /// which they are bound to as opposed to the node type name.
    /// </summary>
    public bool? InferConnectionNameFromField { get; set; }

    /// <summary>
    /// CollectionSegment names are by default inferred from the field name to
    /// which they are bound to as opposed to the node type name.
    /// </summary>
    public bool? InferCollectionSegmentNameFromField { get; set; }

    /// <summary>
    /// The name of the paging provider that shall be used.
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Defines whether a <c>nodes</c> field shall be
    /// exposed on the Connection type, returning the
    /// flattened nodes of the <c>edges</c> field.
    /// </summary>
    public bool? IncludeNodesField { get; set; }

    /// <summary>
    /// Defines whether relative cursors are allowed.
    /// </summary>
    public bool? EnableRelativeCursors { get; set; }

    /// <summary>
    /// Gets or sets the fields that represent relative cursors.
    /// </summary>
    public ImmutableHashSet<string> RelativeCursorFields { get; set; } =
        [
            "pageInfo { forwardCursors }",
            "pageInfo { backwardCursors }"
        ];

    /// <summary>
    /// Gets or sets the fields that represent page infos like hasNextPage or startCursor.
    /// </summary>
    public ImmutableHashSet<string> PageInfoFields { get; set; } =
        [
            "pageInfo { startCursor }",
            "pageInfo { endCursor }" ,
            "pageInfo { hasNextPage }" ,
            "pageInfo { hasPreviousPage }"
        ];

    /// <summary>
    /// Merges the <paramref name="other"/> options into this options instance wherever
    /// a property is not set.
    /// </summary>
    /// <param name="other">
    /// The other options class that shall override unset props.
    /// </param>
    internal void Merge(PagingOptions other)
    {
        DefaultPageSize ??= other.DefaultPageSize;
        MaxPageSize ??= other.MaxPageSize;
        IncludeTotalCount ??= other.IncludeTotalCount;
        AllowBackwardPagination ??= other.AllowBackwardPagination;
        RequirePagingBoundaries ??= other.RequirePagingBoundaries;
        InferConnectionNameFromField ??= other.InferConnectionNameFromField;
        InferCollectionSegmentNameFromField ??= other.InferCollectionSegmentNameFromField;
        ProviderName ??= other.ProviderName;
        IncludeNodesField ??= other.IncludeNodesField;
        EnableRelativeCursors ??= other.EnableRelativeCursors;
        RelativeCursorFields = RelativeCursorFields.Union(other.RelativeCursorFields);
        PageInfoFields = PageInfoFields.Union(other.PageInfoFields);
    }

    /// <summary>
    /// Creates a copy of the current options instance.
    /// </summary>
    internal PagingOptions Copy()
        => new()
        {
            DefaultPageSize = DefaultPageSize,
            MaxPageSize = MaxPageSize,
            IncludeTotalCount = IncludeTotalCount,
            AllowBackwardPagination = AllowBackwardPagination,
            RequirePagingBoundaries = RequirePagingBoundaries,
            InferConnectionNameFromField = InferConnectionNameFromField,
            InferCollectionSegmentNameFromField = InferCollectionSegmentNameFromField,
            ProviderName = ProviderName,
            IncludeNodesField = IncludeNodesField,
            EnableRelativeCursors = EnableRelativeCursors,
            RelativeCursorFields = RelativeCursorFields,
            PageInfoFields = PageInfoFields
        };
}

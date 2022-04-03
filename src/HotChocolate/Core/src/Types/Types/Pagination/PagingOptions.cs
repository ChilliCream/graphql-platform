#nullable enable

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The paging options.
/// </summary>
public struct PagingOptions
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
    /// Defines if the total count of the paged data set
    /// shall be included into the paging result type.
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
    /// Specifies if the GraphQL server shall emulate the PaginationAmount scalar for
    /// the paging navigation arguments.
    /// </summary>
    public bool? LegacySupport { get; set; }

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
        ProviderName ??= other.ProviderName;
        LegacySupport ??= other.LegacySupport;
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
            ProviderName = ProviderName,
            LegacySupport = LegacySupport
        };
}

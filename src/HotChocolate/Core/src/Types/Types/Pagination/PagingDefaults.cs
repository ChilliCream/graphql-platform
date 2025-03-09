namespace HotChocolate.Types.Pagination;

public static class PagingDefaults
{
    public const int DefaultPageSize = 10;

    public const int MaxPageSize = 50;

    public const bool IncludeTotalCount = false;

    public const bool AllowBackwardPagination = true;

    public const bool AllowRelativeCursors = false;

    public const bool InferConnectionNameFromField = true;

    public const bool InferCollectionSegmentNameFromField = true;

    public const bool RequirePagingBoundaries = false;

    public const bool IncludeNodesField = true;

    public static void Apply(PagingOptions options)
    {
        options.DefaultPageSize ??= DefaultPageSize;
        options.MaxPageSize ??= MaxPageSize;
        options.IncludeTotalCount ??= IncludeTotalCount;
        options.AllowBackwardPagination ??= AllowBackwardPagination;
        options.AllowRelativeCursors ??= AllowRelativeCursors;
        options.InferConnectionNameFromField ??= InferConnectionNameFromField;
        options.InferCollectionSegmentNameFromField ??= InferCollectionSegmentNameFromField;
        options.RequirePagingBoundaries ??= RequirePagingBoundaries;
        options.IncludeNodesField ??= IncludeNodesField;
    }
}

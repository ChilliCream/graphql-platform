namespace HotChocolate.Types.Pagination;

public static class PagingDefaults
{
    public const int DefaultPageSize = 10;

    public const int MaxPageSize = 50;

    public const bool IncludeTotalCount = false;

    public const bool AllowBackwardPagination = true;

    public const bool InferConnectionNameFromField = true;

    public const bool InferCollectionSegmentNameFromField = true;

    public const bool RequirePagingBoundaries = false;

    public const bool IncludeNodesField = true;
}

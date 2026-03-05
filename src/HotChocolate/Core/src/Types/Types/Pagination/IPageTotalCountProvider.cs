namespace HotChocolate.Types.Pagination;

public interface IPageTotalCountProvider
{
    int TotalCount { get; }
}

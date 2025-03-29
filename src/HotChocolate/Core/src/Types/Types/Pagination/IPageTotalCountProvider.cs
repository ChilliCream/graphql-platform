#nullable enable

namespace HotChocolate.Types.Pagination;

public interface IPageTotalCountProvider
{
    public int TotalCount { get; }
}

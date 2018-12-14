using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Paging
{
    public interface IPageInfo
    {
        Task<bool> HasNextPageAsync(CancellationToken cancellationToken);
        Task<bool> HasPreviousPageAsync(CancellationToken cancellationToken);
    }
}

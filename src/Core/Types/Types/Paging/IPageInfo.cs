using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Paging
{
    public interface IPageInfo
    {
        bool HasNextPage { get; }

        bool HasPreviousPage { get; }

        string StartCursor { get; }

        string EndCursor { get; }
    }
}

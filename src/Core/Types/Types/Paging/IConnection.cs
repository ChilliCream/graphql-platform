using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Paging
{
    public interface IConnection
    {
        IPageInfo PageInfo { get; }

        IReadOnlyCollection<IEdge> Edges { get; }
    }
}

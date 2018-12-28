using System.Collections.Generic;

namespace HotChocolate.Types.Paging
{
    public interface IConnection
    {
        IPageInfo PageInfo { get; }

        IReadOnlyCollection<IEdge> Edges { get; }
    }
}

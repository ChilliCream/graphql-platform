using System.Collections.Generic;

namespace HotChocolate.Types.Pagination
{
    public interface IConnection
    {
        IPageInfo PageInfo { get; }

        IReadOnlyList<IEdge> Edges { get; }
    }
}

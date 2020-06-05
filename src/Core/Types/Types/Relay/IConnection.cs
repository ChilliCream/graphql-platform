using System.Collections.Generic;

namespace HotChocolate.Types.Relay
{
    public interface IConnection
    {
        IPageInfo PageInfo { get; }

        IReadOnlyList<IEdge> Edges { get; }
    }
}

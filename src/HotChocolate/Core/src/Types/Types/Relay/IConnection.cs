using System.Collections.Generic;

namespace HotChocolate.Types.Relay
{
    public interface IConnection
    {
        IPageInfo PageInfo { get; }

        IReadOnlyCollection<IEdge> Edges { get; }
    }
}

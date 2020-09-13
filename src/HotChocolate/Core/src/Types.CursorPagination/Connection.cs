using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Pagination
{
    public class Connection : IPage
    {
        public Connection(IReadOnlyCollection<IEdge> edges, ConnectionPageInfo info)
        {
            Edges = edges ?? throw new ArgumentNullException(nameof(edges));
            Info = info;
        }

        public IReadOnlyCollection<IEdge> Edges { get; }

        public ConnectionPageInfo Info { get; }

        IReadOnlyCollection<object> IPage.Items => Edges;

        IPageInfo IPage.Info => Info;
    }
}

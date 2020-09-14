using System;
using HotChocolate.Types.Properties;

namespace HotChocolate.Types.Pagination
{
    public class Edge<T> : IEdge
    {
        public Edge(T node, string cursor)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                throw new ArgumentNullException(nameof(cursor));
            }

            Node = node;
            Cursor = cursor;
        }

        public T Node { get; }

        object IEdge.Node => Node;

        public string Cursor { get; }
    }
}

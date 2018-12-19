using System;

namespace HotChocolate.Types.Paging
{
    public class Edge<T>
        : IEdge
    {
        public Edge(string cursor, T node)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The cursor cannot be null or empty.",
                    nameof(cursor));
            }

            Cursor = cursor;
            Node = node;
        }

        public string Cursor { get; }

        public T Node { get; }

        object IEdge.Node => Node;
    }
}

using System;
using HotChocolate.Types.Properties;

namespace HotChocolate.Types.Relay
{
    public class Edge<T> : IEdge
    {
        public Edge(T node, string cursor)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                throw new ArgumentException(
                    CursorResources.Edge_CursorNullOrEmpty,
                    nameof(cursor));
            }

            Node = node;
            Cursor = cursor;
        }

        public T Node { get; }

        object IEdge.Node => Node;

        public string Cursor { get; }
    }
}

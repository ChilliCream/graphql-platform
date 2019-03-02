using System;
using HotChocolate.Properties;

namespace HotChocolate.Types.Relay
{
    public class Edge<T>
        : IEdge
    {
        public Edge(string cursor, T node)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                throw new ArgumentException(
                    TypeResources.Edge_CursorIsNull,
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

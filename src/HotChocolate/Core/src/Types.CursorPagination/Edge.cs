using System.Diagnostics;
using static HotChocolate.Types.Properties.CursorResources;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents an edge in a connection.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Edge<T> : IEdge
{
    private readonly Func<Edge<T>, string>? _resolveCursor;
    private string? _cursor;

    /// <summary>
    /// Initializes a new instance of <see cref="Edge{T}" />.
    /// </summary>
    /// <param name="node">
    /// The node that the edge will wrap.
    /// </param>
    /// <param name="cursor">
    /// The cursor which identifies the <paramref name="node" /> in the current data set.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cursor" /> is <see langword="null" /> or empty.
    /// </exception>
    public Edge(T node, string cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            throw new ArgumentNullException(nameof(cursor));
        }

        Node = node;
        _cursor = cursor;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Edge{T}" />.
    /// </summary>
    /// <param name="node">
    /// The node that the edge will wrap.
    /// </param>
    /// <param name="resolveCursor">
    /// A delegate that resolves the cursor which identifies the <paramref name="node" /> in the data set.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resolveCursor" /> is <see langword="null" />.
    /// </exception>
    public Edge(T node, Func<T, string> resolveCursor)
    {
        if (resolveCursor is null)
        {
            throw new ArgumentNullException(nameof(resolveCursor));
        }

        Node = node;
        _resolveCursor = edge => resolveCursor(edge.Node);
    }

        /// <summary>
    /// Initializes a new instance of <see cref="Edge{T}" />.
    /// </summary>
    /// <param name="node">
    /// The node that the edge will wrap.
    /// </param>
    /// <param name="resolveCursor">
    /// A delegate that resolves the cursor which identifies the <paramref name="node" /> in the data set.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resolveCursor" /> is <see langword="null" />.
    /// </exception>
    public Edge(T node, Func<Edge<T>, string> resolveCursor)
    {
        if (resolveCursor is null)
        {
            throw new ArgumentNullException(nameof(resolveCursor));
        }

        Node = node;
        _resolveCursor = resolveCursor;
    }

    /// <summary>
    /// Gets the node.
    /// </summary>
    public T Node { get; }

    object? IEdge.Node => Node;

    /// <summary>
    /// Gets the cursor which identifies the <see cref="Node" /> in the current data set.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The edge was initialized with a cursor resolver and the resolver returned <see langword="null" />.
    /// </exception>
    public string Cursor
    {
        get
        {
            if (_cursor is null)
            {
                if (_resolveCursor is null)
                {
                    throw new InvalidOperationException(Edge_Cursor_CursorAndResolverNull);
                }

                _cursor = _resolveCursor(this);
                Debug.Assert(_cursor is not null, "The edge's cursor resolver returned null.");
            }

            return _cursor;
        }
    }
}

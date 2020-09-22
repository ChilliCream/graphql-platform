namespace HotChocolate.Types.Pagination
{
    public interface IEdge
    {
        /// <summary>
        /// Gets the cursor which identifies the <see cref="Node" /> in the current data set.
        /// </summary>
        string Cursor { get; }

        /// <summary>
        /// Gets the node.
        /// </summary>
        object? Node { get; }
    }
}

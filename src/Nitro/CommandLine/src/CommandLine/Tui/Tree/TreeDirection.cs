namespace ChilliCream.Nitro.CommandLine.Tui.Tree;

/// <summary>
/// Which way a tree traversal walks a dependency edge from a node.
/// </summary>
internal enum TreeDirection
{
    /// <summary>
    /// Toward what the node depends on.
    /// </summary>
    Up,

    /// <summary>
    /// Toward what depends on the node.
    /// </summary>
    Down
}

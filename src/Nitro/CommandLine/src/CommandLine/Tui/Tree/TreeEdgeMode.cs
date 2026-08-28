using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Tree;

/// <summary>
/// Which dependency edges a tree traversal follows.
/// </summary>
internal enum TreeEdgeMode
{
    /// <summary>
    /// Edges whose type gates readiness, per
    /// <see cref="TaskDependencyTypes.IsBlocking"/>.
    /// </summary>
    Blocking,

    /// <summary>
    /// Only <see cref="TaskDependencyTypes.ParentChild"/> edges.
    /// </summary>
    ParentChild
}

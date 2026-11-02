namespace Mocha;

/// <summary>
/// Controls the traversal behavior of a <see cref="MessagingVisitor{TContext}"/> after visiting a node.
/// </summary>
internal enum VisitorAction
{
    /// <summary>
    /// Continue visiting child nodes and siblings.
    /// </summary>
    Continue,

    /// <summary>
    /// Skip child nodes of the current node but continue with siblings.
    /// </summary>
    Skip,

    /// <summary>
    /// Stop the entire traversal immediately.
    /// </summary>
    Break
}

namespace HotChocolate.Language.Visitors;

/// <summary>
/// Represents a syntax rewriter. A syntax rewriter is a visitor that creates a new syntax tree
/// from the passed in syntax tree.
/// </summary>
/// <typeparam name="TContext">
/// The context type.
/// </typeparam>
public interface ISyntaxRewriter<in TContext> where TContext : ISyntaxVisitorContext
{
    /// <summary>
    /// Rewrite the syntax node.
    /// </summary>
    /// <param name="node">The syntax node that shall be rewritten.</param>
    /// <param name="context">The visitor context.</param>
    /// <returns>
    /// Returns the rewritten <see cref="ISyntaxNode"/>.
    /// </returns>
    ISyntaxNode Rewrite(ISyntaxNode node, TContext context);
}

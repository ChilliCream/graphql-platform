using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Language.Rewriters;

/// <summary>
/// The syntax navigator keeps track of the syntax path that has been traversed and allows to access the nodes in the path in a streamlined way.
/// </summary>
public interface ISyntaxNavigator
{
    /// <summary>
    /// Returns the immediate parent of the current Syntax Node
    /// </summary>
    ISyntaxNode? Parent { get; }
    
    /// <summary>
    /// Adds a syntax node to the Syntax Navigator to record the parent of the Syntax Node being visited.
    /// </summary>
    /// <param name="node">The parent syntax node to be added to the Syntax Navigator</param>
    void Push(ISyntaxNode node);

    /// <summary>
    /// Removes the current parent node from the Syntax Navigator.
    /// </summary>
    /// <returns>The removed parent node.</returns>
    ISyntaxNode Pop();

    /// <summary>
    /// Attempts to remove the current parent node from the Syntax Navigator.
    /// </summary>
    /// <param name="node">The removed parent node.</param>
    /// <returns>True when a syntax node was successfully removed from the Syntax Navigator.
    /// False when a syntax node was not removed.</returns>
    bool TryPop([NotNullWhen(true)] out ISyntaxNode? node);

    /// <summary>
    /// Returns the first ancestor of the provided <see cref="TNode" /> type.
    /// </summary>
    /// <typeparam name="TNode">The type of syntax node to be returned.</typeparam>
    /// <returns>The matching first ancestor or null if no match is found.</returns>
    TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode;

    /// <summary>
    /// Returns all ancestors of the provided <see cref="TNode" /> type.
    /// </summary>
    /// <typeparam name="TNode">The type of syntax nodes to be returned.</typeparam>
    /// <returns>A collection of Syntax Nodes of type <see cref="TNode" /></returns>
    IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode;
}

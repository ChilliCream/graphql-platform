namespace HotChocolate.Language;

/// <summary>
/// Represents a non-terminal node in the GraphQL syntax tree.
/// </summary>
public interface ISyntaxNode
{
    /// <summary>
    /// Returns the <see cref="SyntaxKind"/> of the node.
    /// </summary>
    SyntaxKind Kind { get; }

    /// <summary>
    /// Gets a <see cref="Location"/> of this node in the parsed source text
    /// if available the parser provided this information.
    /// </summary>
    Location? Location { get; }

    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    /// <returns>
    /// Returns the children of this node..
    /// </returns>
    IEnumerable<ISyntaxNode> GetNodes();

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    string ToString();

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    string ToString(bool indented);
}

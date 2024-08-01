namespace HotChocolate.Language.Visitors;

/// <summary>
/// Raised when a required <see cref="ISyntaxNode"/> is rewritten to <see langword="null"/>.
/// </summary>
[Serializable]
public class SyntaxNodeCannotBeNullException : Exception
{
    public SyntaxNodeCannotBeNullException(ISyntaxNode node)
    {
        Kind = node.Kind;
        Location = node.Location;
    }

    /// <summary>
    /// Gets the <see cref="SyntaxKind"/> of the Syntax node
    /// </summary>
    public SyntaxKind Kind { get; }

    /// <summary>
    /// Gets the <see cref="Location"/> of the Syntax node
    /// </summary>
    public Location? Location { get; }
}

using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents list nullability syntax for specifying client-side nullability.
/// </summary>
public sealed class ListNullabilityNode : INullabilityNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListNullabilityNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="element">
    /// The element of the list nullability.
    /// </param>
    public ListNullabilityNode(Location? location, INullabilityNode? element)
    {
        Location = location;
        Element = element;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.ListNullability;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <inheritdoc />
    public INullabilityNode? Element { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Element is not null)
        {
            yield return Element;
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

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
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);
}

using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class SemanticNonNullTypeNode(Location? location, INullableTypeNode type) : ITypeNode
{
    public SemanticNonNullTypeNode(INullableTypeNode type)
        : this(null, type)
    {
    }

    public SyntaxKind Kind => SyntaxKind.SemanticNonNullType;

    public Location? Location { get; } = location;

    public INullableTypeNode Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Type;
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

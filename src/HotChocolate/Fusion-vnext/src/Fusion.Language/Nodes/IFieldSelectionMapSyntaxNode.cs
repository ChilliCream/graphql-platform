namespace HotChocolate.Fusion;

/// <summary>
/// Represents a non-terminal node in the <c>FieldSelectionMap</c> syntax tree.
/// </summary>
internal interface IFieldSelectionMapSyntaxNode
{
    /// <summary>
    /// Returns the <see cref="FieldSelectionMapSyntaxKind"/> of the node.
    /// </summary>
    FieldSelectionMapSyntaxKind Kind { get; }

    /// <summary>
    /// Gets the <see cref="Location"/> of this node in the parsed source text, when provided by the
    /// parser.
    /// </summary>
    Location? Location { get; }

    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    /// <returns>
    /// Returns the children of this node.
    /// </returns>
    IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes();

    /// <summary>
    /// Returns the syntax representation of this <see cref="IFieldSelectionMapSyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the syntax representation of this <see cref="IFieldSelectionMapSyntaxNode"/>.
    /// </returns>
    string ToString();

    /// <summary>
    /// Returns the syntax representation of this <see cref="IFieldSelectionMapSyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the output should be formatted, which includes indenting
    /// nested tokens and adding new lines.
    /// </param>
    /// <returns>
    /// Returns the syntax representation of this <see cref="IFieldSelectionMapSyntaxNode"/>.
    /// </returns>
    string ToString(bool indented);

    /// <summary>
    /// Returns the syntax representation of this <see cref="IFieldSelectionMapSyntaxNode"/>.
    /// </summary>
    /// <param name="options">
    /// Specifies the options used when writing this syntax node.
    /// </param>
    /// <returns>
    /// Returns the syntax representation of this <see cref="IFieldSelectionMapSyntaxNode"/>.
    /// </returns>
    string ToString(StringSyntaxWriterOptions options);
}

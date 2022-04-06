using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// The <see cref="DocumentNode"/> represents a parsed GraphQL document
/// which also is the root node of a parsed GraphQL document.
/// </para>
/// <para>The document can contain schema definition nodes or query nodes.</para>
/// </summary>
public sealed class DocumentNode : ISyntaxNode, IEquatable<DocumentNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="DocumentNode"/>.
    /// </summary>
    /// <param name="definitions">
    /// The GraphQL definitions this document contains.
    /// </param>
    public DocumentNode(
        IReadOnlyList<IDefinitionNode> definitions)
        : this(null, definitions)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the document in the parsed source text.
    /// </param>
    /// <param name="definitions">
    /// The GraphQL definitions this document contains.
    /// </param>
    public DocumentNode(
        Location? location,
        IReadOnlyList<IDefinitionNode> definitions)
    {
        Location = location;
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.Document;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the documents definitions.
    /// </summary>
    public IReadOnlyList<IDefinitionNode> Definitions { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => Definitions;

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

    /// <summary>
    /// Creates a new instance that has all the characteristics of this
    /// documents but with a different location.
    /// </summary>
    /// <param name="location">
    /// The location that shall be applied to the new document.
    /// </param>
    /// <returns>
    /// Returns a new instance that has all the characteristics of this
    /// documents but with a different location.
    /// </returns>
    public DocumentNode WithLocation(Location? location)
        => new(location, Definitions);

    /// <summary>
    /// Creates a new instance that has all the characteristics of this
    /// documents but with different definitions.
    /// </summary>
    /// <param name="definitions">
    /// The definitions that shall be applied to the new document.
    /// </param>
    /// <returns>
    /// Returns a new instance that has all the characteristics of this
    /// documents but with a different definitions.
    /// </returns>
    public DocumentNode WithDefinitions(IReadOnlyList<IDefinitionNode> definitions)
        => new(Location, definitions);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other" /> parameter;
    /// otherwise, false.
    /// </returns>
    public bool Equals(DocumentNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EqualityHelper.Equals(Definitions, other.Definitions);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current object.
    /// </param>
    /// <returns>
    /// true if the specified object  is equal to the current object; otherwise, false.
    /// </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            (obj is DocumentNode other && Equals(other));

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Kind);
        hashCode.AddNodes(Definitions);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// The equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.
    /// </returns>
    public static bool operator ==(DocumentNode? left, DocumentNode? right)
        => Equals(left, right);
        
    /// <summary>
    /// The not equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal.
    /// </returns>
    public static bool operator !=(DocumentNode? left, DocumentNode? right)
        => !Equals(left, right);

    /// <summary>
    /// Gets an empty GraphQL document.
    /// </summary>
    public static DocumentNode Empty { get; } =
        new(null, Array.Empty<IDefinitionNode>());
}

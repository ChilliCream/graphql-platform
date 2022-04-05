using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents a named type syntax.
/// </summary>
public sealed class NamedTypeNode
    : INullableTypeNode
    , IEquatable<NamedTypeNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="NamedTypeNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the type.
    /// </param>
    public NamedTypeNode(string name)
        : this(null, new NameNode(name))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NamedTypeNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the type.
    /// </param>
    public NamedTypeNode(NameNode name)
        : this(null, name)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NamedTypeNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the type.
    /// </param>
    public NamedTypeNode(Location? location, NameNode name)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.NamedType;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the type name.
    /// </summary>
    public NameNode Name { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;
    }

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public NamedTypeNode WithLocation(Location? location) => new(location, Name);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current <see cref="NamedSyntaxNode.Name" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public NamedTypeNode WithName(NameNode name) => new(Location, name);

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
    public bool Equals(NamedTypeNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name.Equals(other.Name);
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
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return Equals(obj as NamedTypeNode);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(Kind, Name);

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

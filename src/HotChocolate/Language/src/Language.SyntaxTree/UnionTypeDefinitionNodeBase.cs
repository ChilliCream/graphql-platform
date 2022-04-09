using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for union type and union type extension.
/// </summary>
public abstract class UnionTypeDefinitionNodeBase
    : NamedSyntaxNode
    , IEquatable<UnionTypeDefinitionNodeBase>
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the input object.
    /// </param>
    /// <param name="directives">
    /// The directives of this input object.
    /// </param>
    /// <param name="types">
    /// The types of the union type.
    /// </param>
    protected UnionTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> types)
        : base(location, name, directives)
    {
        Types = types ?? throw new ArgumentNullException(nameof(types));
    }

    /// <summary>
    /// Gets the types of the union type.
    /// </summary>
    public IReadOnlyList<NamedTypeNode> Types { get; }

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
    public bool Equals(UnionTypeDefinitionNodeBase? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) && other.Types.IsEqualTo(Types);
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
            (obj is UnionTypeDefinitionNodeBase other && Equals(other));

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.AddNodes(Types);
        return hashCode.ToHashCode();
    }
}

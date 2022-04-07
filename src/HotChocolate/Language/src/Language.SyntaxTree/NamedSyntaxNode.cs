using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for named syntax nodes.
/// </summary>
public abstract class NamedSyntaxNode : INamedSyntaxNode, IEquatable<NamedSyntaxNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="NamedSyntaxNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="directives">
    /// The directives that are annotated to this syntax node.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    protected NamedSyntaxNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Directives = directives ?? throw new ArgumentNullException(nameof(directives));
    }

    /// <inheritdoc />
    public abstract SyntaxKind Kind { get; }

    /// <inheritdoc />
    public Location? Location { get; }

    /// <inheritdoc />
    public NameNode Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<DirectiveNode> Directives { get; }

    /// <inheritdoc />
    public abstract IEnumerable<ISyntaxNode> GetNodes();

    /// <inheritdoc />
    public abstract string ToString(bool indented);

    /// <inheritdoc />
    public abstract override string ToString();

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
    public bool Equals(NamedSyntaxNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name.IsEqualTo(other.Name) &&
            Directives.IsEqualTo(other.Directives);
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
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((NamedSyntaxNode)obj);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Name);
        hashCode.AddNodes(Directives);
        return hashCode.ToHashCode();
    }
}

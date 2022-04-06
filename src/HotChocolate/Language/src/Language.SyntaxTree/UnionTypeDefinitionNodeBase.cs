using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

public abstract class UnionTypeDefinitionNodeBase
    : NamedSyntaxNode
    , IEqualityComparer<UnionTypeDefinitionNodeBase>
{
    protected UnionTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> types)
        : base(location, name, directives)
    {
        Types = types ?? throw new ArgumentNullException(nameof(types));
    }

    public IReadOnlyList<NamedTypeNode> Types { get; }

    /// <summary>
    /// Indicates whether one object <paramref name="x" /> is equal to another object of the same type <paramref name="y" />.
    /// </summary>
    /// <param name="x">
    /// An initial object to compare.
    /// </param>
    /// <param name="y">
    /// An second object to compare with.
    /// </param>
    /// <returns>
    /// true if the parameter <paramref name="x" />is equal to the <paramref name="y" /> parameter;
    /// otherwise, false.
    /// </returns>
    public bool Equals(UnionTypeDefinitionNodeBase? x, UnionTypeDefinitionNodeBase? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null))
        {
            return false;
        }

        if (ReferenceEquals(y, null))
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Types.IsEqualTo(y.Types);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public int GetHashCode(UnionTypeDefinitionNodeBase obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.AddNodes(obj.Types);
        return hashCode.ToHashCode();
    }
}

using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class InterfaceTypeExtensionNode
    : InterfaceTypeDefinitionNodeBase
    , ITypeExtensionNode
    , IEquatable<InterfaceTypeExtensionNode>
{
    public InterfaceTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields)
        : base(location, name, directives, interfaces, fields)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.InterfaceTypeExtension;

    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (NamedTypeNode interfaceName in Interfaces)
        {
            yield return interfaceName;
        }

        foreach (DirectiveNode directive in Directives)
        {
            yield return directive;
        }

        foreach (FieldDefinitionNode field in Fields)
        {
            yield return field;
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
    public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    public InterfaceTypeExtensionNode WithLocation(Location? location)
        => new(location, Name, Directives, Interfaces, Fields);

    public InterfaceTypeExtensionNode WithName(NameNode name)
        => new(Location, name, Directives, Interfaces, Fields);

    public InterfaceTypeExtensionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, directives, Interfaces, Fields);

    public InterfaceTypeExtensionNode WithFields(IReadOnlyList<FieldDefinitionNode> fields)
        => new(Location, Name, Directives, Interfaces, fields);

    public InterfaceTypeExtensionNode WithInterfaces(IReadOnlyList<NamedTypeNode> interfaces)
        => new(Location, Name, Directives, interfaces, Fields);

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
    public bool Equals(InterfaceTypeExtensionNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other)
               && Kind == other.Kind;
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
            (obj is InterfaceTypeExtensionNode other && Equals(other));

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), Kind);

    public static bool operator ==(
        InterfaceTypeExtensionNode? left,
        InterfaceTypeExtensionNode? right)
        => Equals(left, right);

    public static bool operator !=(
        InterfaceTypeExtensionNode? left,
        InterfaceTypeExtensionNode? right)
        => !Equals(left, right);
}

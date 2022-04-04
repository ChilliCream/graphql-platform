using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class FragmentSpreadNode
    : NamedSyntaxNode
    , ISelectionNode
    , IEquatable<FragmentSpreadNode>
{
    public FragmentSpreadNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    { }

    public override SyntaxKind Kind { get; } = SyntaxKind.FragmentSpread;

    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (DirectiveNode directive in Directives)
        {
            yield return directive;
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

    public FragmentSpreadNode WithLocation(Location? location)
    {
        return new FragmentSpreadNode(location, Name, Directives);
    }

    public FragmentSpreadNode WithName(NameNode name)
    {
        return new FragmentSpreadNode(Location, name, Directives);
    }

    public FragmentSpreadNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
    {
        return new FragmentSpreadNode(Location, Name, directives);
    }

    public bool Equals(FragmentSpreadNode? other)
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

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is FragmentSpreadNode other
            && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), (int)Kind);
    }

    public static bool operator ==(
        FragmentSpreadNode? left,
        FragmentSpreadNode? right)
        => Equals(left, right);

    public static bool operator !=(
        FragmentSpreadNode? left,
        FragmentSpreadNode? right)
        => !Equals(left, right);
}

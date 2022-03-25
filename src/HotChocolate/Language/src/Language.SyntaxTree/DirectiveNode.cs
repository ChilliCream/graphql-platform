using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class DirectiveNode : ISyntaxNode, IEquatable<DirectiveNode>
{
    public DirectiveNode(
        string name,
        params ArgumentNode[] arguments)
        : this(new NameNode(name), arguments)
    {
    }

    public DirectiveNode(
        string name,
        IReadOnlyList<ArgumentNode> arguments)
        : this(new NameNode(name), arguments)
    {
    }

    public DirectiveNode(
        NameNode name,
        IReadOnlyList<ArgumentNode> arguments)
        : this(null, name, arguments)
    {
    }

    public DirectiveNode(
        Location? location,
        NameNode name,
        IReadOnlyList<ArgumentNode> arguments)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }

    public SyntaxKind Kind => SyntaxKind.Directive;

    public Location? Location { get; }

    public NameNode Name { get; }

    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (ArgumentNode argument in Arguments)
        {
            yield return argument;
        }
    }

    public IReadOnlyList<ArgumentNode> Arguments { get; }

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

    public DirectiveNode WithLocation(Location? location)
        => new(location, Name, Arguments);

    public DirectiveNode WithName(NameNode name)
        => new(Location, name, Arguments);

    public DirectiveNode WithArguments(IReadOnlyList<ArgumentNode> arguments)
        => new(Location, Name, arguments);

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
    public bool Equals(DirectiveNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Kind == other.Kind &&
            Equals(Location, other.Location) &&
            Name.Equals(other.Name) &&
            EqualityHelper.Equals(Arguments, other.Arguments);
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
            obj is DirectiveNode other &&
            Equals(other);

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int) Kind;
            hashCode = (hashCode * 397) ^ (Location != null ? Location.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ EqualityHelper.GetHashCode(Arguments);
            return hashCode;
        }
    }

    public static bool operator ==(DirectiveNode? left, DirectiveNode? right)
        => Equals(left, right);

    public static bool operator !=(DirectiveNode? left, DirectiveNode? right)
        => !Equals(left, right);
}

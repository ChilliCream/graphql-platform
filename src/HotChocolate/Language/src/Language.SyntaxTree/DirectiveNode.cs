using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents an applied directive.
/// </summary>
public sealed class DirectiveNode : ISyntaxNode, IEquatable<DirectiveNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the directive.
    /// </param>
    /// <param name="arguments">
    /// The argument values of this directive.
    /// </param>
    public DirectiveNode(
        string name,
        params ArgumentNode[] arguments)
        : this(new NameNode(name), arguments)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the directive.
    /// </param>
    /// <param name="arguments">
    /// The argument values of this directive.
    /// </param>
    public DirectiveNode(
        string name,
        IReadOnlyList<ArgumentNode> arguments)
        : this(new NameNode(name), arguments)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the directive.
    /// </param>
    /// <param name="arguments">
    /// The argument values of this directive.
    /// </param>
    public DirectiveNode(
        NameNode name,
        IReadOnlyList<ArgumentNode> arguments)
        : this(null, name, arguments)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the directive.
    /// </param>
    /// <param name="arguments">
    /// The argument values of this directive.
    /// </param>
    public DirectiveNode(
        Location? location,
        NameNode name,
        IReadOnlyList<ArgumentNode> arguments)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.Directive;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the name of this directive.
    /// </summary>
    public NameNode Name { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (ArgumentNode argument in Arguments)
        {
            yield return argument;
        }
    }

    /// <summary>
    /// Gets the argument values of this directive.
    /// </summary>
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
    public DirectiveNode WithLocation(Location? location)
        => new(location, Name, Arguments);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public DirectiveNode WithName(NameNode name)
        => new(Location, name, Arguments);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Arguments" /> with <paramref name="arguments" />.
    /// </summary>
    /// <param name="arguments">
    /// The arguments that shall be used to replace the current arguments.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="arguments" />.
    /// </returns>
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
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name.Equals(other.Name) &&
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
            (obj is DirectiveNode other && Equals(other));

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
        hashCode.Add(Name);
        hashCode.AddNodes(Arguments);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(DirectiveNode? left, DirectiveNode? right)
        => Equals(left, right);

    public static bool operator !=(DirectiveNode? left, DirectiveNode? right)
        => !Equals(left, right);
}

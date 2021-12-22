using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;
using static HotChocolate.Language.Utilities.ThrowHelper;

namespace HotChocolate.Language;

/// <summary>
/// A <see cref="SchemaCoordinateNode"/> is a human readable string that uniquely identifies a
/// schema element within a GraphQL Schema.
/// A schema element is a specific instance of a named type, field, input field, enum value,
/// field argument, directive, or directive argument.
/// A <see cref="SchemaCoordinateNode"/> is always unique. Each schema element may be referenced
/// by exactly one possible schema coordinate.
///
/// A <see cref="SchemaCoordinateNode"/> may refer to either a defined or built-in schema element.
/// For example, `String` and `@deprecated(reason:)` are both valid schema coordinates which refer
/// to built-in schema elements. However it must not refer to a meta-field.
/// For example, `Business.__typename` is <b>not</b> a valid schema coordinate.
///
/// SchemaCoordinate :
///  - Name
///  - Name . Name
///  - Name . Name ( Name : )
///  - @ Name
///  - @ Name ( Name : )
///
/// <remarks>
/// Note: A <see cref="SchemaCoordinateNode"/> is not a definition within a GraphQL
/// <see cref="DocumentNode"/>, but a separate standalone grammar, intended to be used by tools
/// to reference types, fields, and other schema elements. For example as references within
/// documentation, or as lookup keys in usage frequency tracking.
/// </remarks>
/// </summary>
public sealed class SchemaCoordinateNode : ISyntaxNode
{
    /// <summary>
    /// Creates a new instance of <see cref="SchemaCoordinateNode"/>
    /// </summary>
    public SchemaCoordinateNode(
        Location? location,
        bool ofDirective,
        NameNode name,
        NameNode? memberName,
        NameNode? argumentName)
    {
        if (ofDirective && memberName is not null)
        {
            throw SchemaCoordinate_MemberNameCannotBeSetOnADirectiveCoordinate(nameof(memberName));
        }

        if (!ofDirective && memberName is null && argumentName is not null)
        {
            throw SchemaCoordinate_ArgumentNameCannotBeSetWithoutMemberName(nameof(memberName));
        }

        Location = location;
        OfDirective = ofDirective;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        MemberName = memberName;
        ArgumentName = argumentName;
    }

    /// <inheritdoc />
    public Location? Location { get; }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.SchemaCoordinate;

    /// <summary>
    /// Specifies if this <see cref="SchemaCoordinateNode"/> is a coordinate of a directive.
    /// </summary>
    public bool OfDirective { get; }

    /// <summary>
    /// The name of the referenced <see cref="INamedSyntaxNode"/>
    /// </summary>
    public NameNode Name { get; }

    /// <summary>
    /// The optional name of the referenced field or enum value
    /// </summary>
    public NameNode? MemberName { get; }

    /// <summary>
    /// The optional name of the referenced argument
    /// </summary>
    public NameNode? ArgumentName { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        if (MemberName is not null)
        {
            yield return MemberName;
        }

        if (ArgumentName is not null)
        {
            yield return ArgumentName;
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
    public SchemaCoordinateNode WithLocation(Location? location)
        => new(location, OfDirective, Name, MemberName, ArgumentName);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="OfDirective" /> with <paramref name="ofDirective" />.
    /// </summary>
    /// <param name="ofDirective">
    /// The ofDirective that shall be used to replace the current ofDirective.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="ofDirective" />.
    /// </returns>
    public SchemaCoordinateNode WithOfDirective(bool ofDirective)
        => new(Location, ofDirective, Name, MemberName, ArgumentName);

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
    public SchemaCoordinateNode WithName(NameNode name)
        => new(Location, OfDirective, name, MemberName, ArgumentName);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="MemberName" /> with <paramref name="memberName" />.
    /// </summary>
    /// <param name="memberName">
    /// The memberName that shall be used to replace the current memberName.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="memberName" />.
    /// </returns>
    public SchemaCoordinateNode WithMemberName(NameNode? memberName)
        => new(Location, OfDirective, Name, memberName, ArgumentName);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="ArgumentName" /> with <paramref name="argumentName" />.
    /// </summary>
    /// <param name="argumentName">
    /// The argumentName that shall be used to replace the current argumentName.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="argumentName" />.
    /// </returns>
    public SchemaCoordinateNode WithArgumentName(NameNode? argumentName)
        => new(Location, OfDirective, Name, MemberName, argumentName);
}

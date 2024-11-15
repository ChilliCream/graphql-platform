using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static System.StringComparison;
using static HotChocolate.Properties.PrimitivesResources;

namespace HotChocolate;

/// <summary>
/// <para>
/// A <see cref="SchemaCoordinate"/> is a human-readable string that uniquely identifies a
/// schema element within a GraphQL Schema.
/// A schema element is a specific instance of a named type, field, input field, enum value,
/// field argument, directive, or directive argument.
/// A <see cref="SchemaCoordinate"/> is always unique. Each schema element may be referenced
/// by exactly one possible schema coordinate.
/// </para>
/// <para>
/// A <see cref="SchemaCoordinate"/> may refer to either a defined or built-in schema element.
/// For example, `String` and `@deprecated(reason:)` are both valid schema coordinates which refer
/// to built-in schema elements. However it must not refer to a meta-field.
/// For example, `Business.__typename` is <b>not</b> a valid schema coordinate.
/// </para>
/// <para>
/// SchemaCoordinate:
///  - Name
///  - Name.Name
///  - Name.Name(Name:)
///  - @Name
///  - @Name (Name:)
/// </para>
/// </summary>
public readonly struct SchemaCoordinate : IEquatable<SchemaCoordinate>
{
    /// <summary>
    /// Creates a new instance of <see cref="SchemaCoordinate"/>
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="name"/> is <c>null</c> or <see cref="string.Empty" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - A directive cannot contain a <paramref name="memberName"/>.
    /// - A <paramref name="argumentName"/>. without a <paramref name="memberName"/> is only allowed
    /// on directive coordinates.
    /// </exception>
    public SchemaCoordinate(
        string name,
        string? memberName = null,
        string? argumentName = null,
        bool ofDirective = false)
    {
        memberName?.EnsureGraphQLName(nameof(memberName));
        argumentName?.EnsureGraphQLName(nameof(argumentName));

        if (ofDirective && memberName is not null)
        {
            throw new ArgumentException(
                ThrowHelper_SchemaCoordinate_MemberNameCannotBeSetOnADirectiveCoordinate,
                nameof(memberName));
        }

        if (!ofDirective && memberName is null && argumentName is not null)
        {
            throw new ArgumentException(
                ThrowHelper_SchemaCoordinate_ArgumentNameCannotBeSetWithoutMemberName,
                nameof(argumentName));
        }

        Name = name.EnsureGraphQLName();
        MemberName = memberName;
        ArgumentName = argumentName;
        OfDirective = ofDirective;
    }

    /// <summary>
    /// Specifies if this <see cref="SchemaCoordinateNode"/> is a coordinate of a directive.
    /// </summary>
    public bool OfDirective { get; }

    /// <summary>
    /// The name of the referenced <see cref="INamedSyntaxNode"/>
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The optional name of the referenced field or enum value
    /// </summary>
    public string? MemberName { get; }

    /// <summary>
    /// The optional name of the referenced argument
    /// </summary>
    public string? ArgumentName { get; }

    /// <summary>
    /// Gets the syntax representation of this <see cref="SchemaCoordinate"/>.
    /// </summary>
    public SchemaCoordinateNode ToSyntax()
    {
        NameNode? memberName = MemberName is null ? null : new(MemberName);
        NameNode? argumentName = ArgumentName is null ? null : new(ArgumentName);
        return new(null, OfDirective, new(Name), memberName, argumentName);
    }

    /// <summary>
    /// Gets the string representation of this <see cref="SchemaCoordinate"/>.
    /// </summary>
    public override string ToString() => ToSyntax().ToString();

    /// <summary>
    /// Tries to parse a <see cref="SchemaCoordinate"/> from a <see cref="string"/>.
    /// </summary>
    /// <param name="s">The string that may represent a <see cref="SchemaCoordinate"/>.</param>
    /// <param name="coordinate">
    /// If the string <paramref name="s"/> represented a valid schema coordinate string this
    /// will be the parsed schema coordinate.
    /// </param>
    /// <returns>
    /// <c>true</c> if the string was a valid representation of a schema coordinate.
    /// </returns>
    public static bool TryParse(
        string s,
        [NotNullWhen(true)] out SchemaCoordinate? coordinate)
    {
        if (string.IsNullOrEmpty(s))
        {
            coordinate = null;
            return false;
        }

        try
        {
            coordinate = Parse(s);
            return true;
        }
        catch (SyntaxException)
        {
            coordinate = null;
            return false;
        }
    }

    /// <summary>
    /// Parses a schema coordinate string representation.
    /// </summary>
    /// <param name="s">The schema coordinate string representation.</param>
    /// <returns>
    /// Returns the parses schema coordinate.
    /// </returns>
    public static SchemaCoordinate Parse(string s)
        => FromSyntax(Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(s));

    /// <summary>
    /// Creates a <see cref="SchemaCoordinate"/> from a <see cref="SchemaCoordinateNode"/>.
    /// </summary>
    /// <param name="node">
    /// The syntax node.
    /// </param>
    /// <returns>
    /// Returns the <see cref="SchemaCoordinate"/> instance.
    /// </returns>
    public static SchemaCoordinate FromSyntax(SchemaCoordinateNode node)
    {
        var memberName = node.MemberName?.Value;
        var argumentName = node.ArgumentName?.Value;
        return new(node.Name.Value, memberName, argumentName, node.OfDirective);
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// <c>true</c> if the current object is equal to the <paramref name="other" /> parameter;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(SchemaCoordinate other)
        => OfDirective == other.OfDirective &&
            string.Equals(Name, other.Name, Ordinal) &&
            Nullable.Equals(MemberName, other.MemberName) &&
            Nullable.Equals(ArgumentName, other.ArgumentName);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="obj">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// <c>true</c> if the current object is equal to the <paramref name="obj" /> parameter;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is SchemaCoordinate other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that is the hash code for this instance.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(OfDirective, Name, MemberName, ArgumentName);

    public static bool operator ==(SchemaCoordinate left, SchemaCoordinate right)
        => left.Equals(right);

    public static bool operator !=(SchemaCoordinate left, SchemaCoordinate right)
        => !left.Equals(right);

    public static implicit operator SchemaCoordinate(string s) => Parse(s);
}

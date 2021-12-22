using System;
using System.Security.AccessControl;
using System.Text;
using HotChocolate.Abstractions;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate;

/// <summary>
/// A <see cref="SchemaCoordinate"/> uniquely identifies a schema element within a GraphQL Schema.
///
/// A schema element is a specific instance of a named type, field, input field, enum value,
/// field argument, directive, or directive argument.
///
/// A <see cref="SchemaCoordinate"/> is always unique. Each schema element may be referenced
/// by exactly one possible schema coordinate.
/// </summary>
public readonly struct SchemaCoordinate : IEquatable<SchemaCoordinate>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaCoordinate"/>.
    /// </summary>
    /// <param name="ofDirective">
    /// Defines if this schema coordinate references a directive
    /// </param>
    /// <param name="name">
    /// The type name.
    /// </param>
    /// <param name="memberName">
    /// The optional member name.
    /// </param>
    /// <param name="argumentName">
    /// The optional argument name.
    /// </param>
    public SchemaCoordinate(
        bool ofDirective,
        NameString name,
        NameString? memberName = null,
        NameString? argumentName = null)
    {
        if (ofDirective && !(!memberName.HasValue || memberName.Value.IsEmpty))
        {
            throw ThrowHelper
                .SchemaCoordinate_MemberNameCannotBeSetOnADirectiveCoordinate(nameof(memberName));
        }

        if (!ofDirective &&
            (!memberName.HasValue || memberName.Value.IsEmpty) &&
            !(!argumentName.HasValue || argumentName.Value.IsEmpty))
        {
            throw ThrowHelper
                .SchemaCoordinate_ArgumentNameCannotBeSetWithoutMemberName(nameof(memberName));
        }

        OfDirective = ofDirective;
        Name = name.EnsureNotEmpty(nameof(name));
        MemberName = memberName;
        ArgumentName = argumentName;
    }

    /// <summary>
    /// Deconstructs this type into its parts
    /// </summary>
    public void Deconstruct(
        out bool ofDirective,
        out NameString name,
        out NameString? memberName,
        out NameString? argumentName)
    {
        ofDirective = OfDirective;
        name = Name;
        memberName = MemberName;
        argumentName = ArgumentName;
    }

    /// <summary>
    /// Gets the name to which this schema coordinate is referring to.
    /// </summary>
    public NameString Name { get; }

    /// <summary>
    /// Defines if this schema coordinate references a directive
    /// </summary>
    public bool OfDirective { get; }

    /// <summary>
    /// Gets the member name to which this schema coordinate is referring to.
    /// <remarks>
    /// The member name can be null if the coordinate is just referring to a type or directive.
    /// </remarks>
    /// </summary>
    public NameString? MemberName { get; }

    /// <summary>
    /// Gets the argument name to which this schema coordinate is referring to.
    /// <remarks>
    /// The argument name can be null if the coordinate is just referring to a field.
    /// </remarks>
    /// </summary>
    public NameString? ArgumentName { get; }

    /// <summary>
    /// Create a new schema coordinate based on the current one.
    /// </summary>
    public SchemaCoordinate With(
        Optional<bool> ofDirective = default,
        Optional<NameString> name = default,
        Optional<NameString?> memberName = default,
        Optional<NameString?> argumentName = default)
    {
        return new(
            ofDirective.HasValue ? ofDirective.Value : OfDirective,
            name.HasValue ? name.Value : Name,
            memberName.HasValue ? memberName.Value : MemberName,
            argumentName.HasValue ? argumentName.Value : ArgumentName);
    }

    /// <summary>
    /// Indicates whether the current schema coordinate is equal
    /// to another schema coordinate of the same type.
    /// </summary>
    /// <param name="other">
    /// A schema coordinate to compare with this schema coordinate.
    /// </param>
    /// <returns>
    /// true if the current schema coordinate is equal to the
    /// <paramref name="other">other</paramref> parameter;
    /// otherwise, false.
    /// </returns>
    public bool Equals(SchemaCoordinate other)
    {
        return Nullable.Equals(OfDirective, other.OfDirective) &&
            Name.Equals(other.Name) &&
            Nullable.Equals(ArgumentName, other.ArgumentName) &&
            Nullable.Equals(MemberName, other.MemberName);
    }

    /// <summary>
    /// Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current instance.
    /// </param>
    /// <returns>
    /// true if <paramref name="obj">obj</paramref> and this instance
    /// are the same type and represent the same value; otherwise, false.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is SchemaCoordinate other && Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that is the hash code for this instance.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = OfDirective.GetHashCode();
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ MemberName.GetHashCode();
            hashCode = (hashCode * 397) ^ ArgumentName.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns the string representation of this schema coordinate.
    /// </summary>
    /// <returns>
    /// A fully qualified schema reference string.
    /// </returns>
    public override string ToString()
    {
        StringBuilder builder = new();
        if (OfDirective)
        {
            builder.Append('@');
        }

        builder.Append(Name);

        if (MemberName is { IsEmpty: false })
        {
            builder.Append('.');
            builder.Append(MemberName);
        }

        if (ArgumentName is { IsEmpty: false })
        {
            builder.Append('(');
            builder.Append(ArgumentName);
            builder.Append(":)");
        }

        return builder.ToString();
    }
}

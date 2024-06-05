using System;
using HotChocolate.Properties;
using HotChocolate.Utilities;
using static System.StringComparison;

namespace HotChocolate;

/// <summary>
/// A field in graphql is uniquely located within a parent type and hence code elements
/// need to be specified using those coordinates.
/// </summary>
public readonly struct FieldCoordinate : IEquatable<FieldCoordinate>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FieldCoordinate"/>.
    /// </summary>
    /// <param name="typeName">
    /// The type name.
    /// </param>
    /// <param name="fieldName">
    /// The field name.
    /// </param>
    /// <param name="argumentName">
    /// The optional argument name.
    /// </param>
    public FieldCoordinate(
        string typeName,
        string fieldName,
        string? argumentName = null)
    {
        TypeName = typeName.EnsureGraphQLName();
        FieldName = fieldName.EnsureGraphQLName();
        ArgumentName = argumentName?.EnsureGraphQLName();
        HasValue = true;
    }

    /// <summary>
    /// Deconstructs this type into its parts
    /// </summary>
    public void Deconstruct(
        out string typeName,
        out string fieldName,
        out string? argumentName)
    {
        typeName = TypeName;
        fieldName = FieldName;
        argumentName = ArgumentName;
    }

    /// <summary>
    /// Creates a field coordinate that is missing the type name which is later filled in.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="argumentName">The argument name.</param>
    /// <returns></returns>
    public static FieldCoordinate CreateWithoutType(
        string fieldName,
        string? argumentName = null) =>
        new("__Empty", fieldName, argumentName);

    /// <summary>
    /// Defines if this field coordinate is empty.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets the type name to which this field coordinate is referring to.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the field name to which this field coordinate is referring to.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets the argument name to which this field coordinate is referring to.
    /// Note: the argument name can be null if the coordinate is just referring to a field.
    /// </summary>
    public string? ArgumentName { get; }

    /// <summary>
    /// Create a new field coordinate based on the current one.
    /// </summary>
    public FieldCoordinate With(
        Optional<string> typeName = default,
        Optional<string> fieldName = default,
        Optional<string?> argumentName = default)
        #if NET5_0_OR_GREATER
        => new(
            typeName.HasValue ? typeName.Value : TypeName,
            fieldName.HasValue ? fieldName.Value : FieldName,
            argumentName.HasValue ? argumentName.Value : ArgumentName);
        #else
        => new(
            typeName.HasValue ? typeName.Value! : TypeName,
            fieldName.HasValue ? fieldName.Value! : FieldName,
            argumentName.HasValue ? argumentName.Value : ArgumentName);
        #endif

    /// <summary>
    /// Indicates whether the current field coordinate is equal
    /// to another field coordinate of the same type.
    /// </summary>
    /// <param name="other">
    /// A field coordinate to compare with this field coordinate.
    /// </param>
    /// <returns>
    /// true if the current field coordinate is equal to the
    /// <paramref name="other">other</paramref> parameter;
    /// otherwise, false.
    /// </returns>
    public bool Equals(FieldCoordinate other)
    {
        return string.Equals(TypeName, other.TypeName, Ordinal) &&
            string.Equals(FieldName, other.FieldName, Ordinal) &&
            string.Equals(ArgumentName, other.ArgumentName, Ordinal);
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
        => obj is FieldCoordinate other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that is the hash code for this instance.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(TypeName, FieldName, ArgumentName);

    /// <summary>
    /// Returns the string representation of this field coordinate.
    /// </summary>
    /// <returns>
    /// A fully qualified field reference string.
    /// </returns>
    public override string ToString()
        => ArgumentName is null
            ? $"{TypeName}.{FieldName}"
            : $"{TypeName}.{FieldName}({ArgumentName})";

    /// <summary>
    /// Converts a field coordinate string into a <see cref="FieldCoordinate"/> instance.
    /// </summary>
    /// <param name="s">
    /// The field coordinate string.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The format of <paramref name="s"/> is wrong.
    /// </exception>
    public static implicit operator FieldCoordinate(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        var parts = s.Split('.');

        if (parts.Length != 2)
        {
            throw new ArgumentException(
                AbstractionResources.FieldCoordinate_Parse_InvalidComponentCount,
                nameof(s));
        }

        var fieldParts = parts[1].Split('(');

        if (fieldParts.Length > 2)
        {
            throw new ArgumentException(
                AbstractionResources.FieldCoordinate_Parse_InvalidFieldComponentCount,
                nameof(s));
        }

        if (fieldParts.Length == 1)
        {
            return new FieldCoordinate(parts[0], parts[1]);
        }

        if (fieldParts.Length == 2)
        {
            return new FieldCoordinate(
                parts[0],
                fieldParts[0],
                fieldParts[1].TrimEnd().TrimEnd(')'));
        }

        throw new ArgumentException(
            AbstractionResources.FieldCoordinate_Parse_InvalidFormat,
            nameof(s));
    }
}

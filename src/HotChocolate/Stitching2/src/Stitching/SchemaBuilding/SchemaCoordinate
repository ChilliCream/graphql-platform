using System;

namespace HotChocolate.Stitching.SchemaBuilding
{
    /// <summary>
    /// A schema coordinate identifies a field or a type in a schema.
    /// </summary>
    public readonly struct SchemaCoordinate : IEquatable<SchemaCoordinate>
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
        public SchemaCoordinate(
            NameString typeName,
            NameString? fieldName = null)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            FieldName = fieldName;
            HasValue = true;
        }

        /// <summary>
        /// Deconstructs this type into its parts
        /// </summary>
        public void Deconstruct(
            out NameString typeName,
            out NameString? fieldName)
        {
            typeName = TypeName;
            fieldName = FieldName;
        }

        /// <summary>
        /// Defines if this schema coordinate is empty.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the type name to which this coordinate is referring to.
        /// </summary>
        public NameString TypeName { get; }

        /// <summary>
        /// Gets the field name to which this coordinate is referring to.
        /// </summary>
        public NameString? FieldName { get; }

        /// <summary>
        /// Create a new schema coordinate based on the current one.
        /// </summary>
        public SchemaCoordinate With(
            Optional<NameString> typeName = default,
            Optional<NameString?> fieldName = default)
        {
            return new(
                typeName.HasValue ? typeName.Value : TypeName,
                fieldName.HasValue ? fieldName.Value : FieldName);
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
            return TypeName.Equals(other.TypeName) &&
                   FieldName.Equals(other.FieldName);
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
            return obj is FieldCoordinate other && Equals(other);
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
                var hashCode = TypeName.GetHashCode();
                hashCode = (hashCode * 397) ^ FieldName.GetHashCode();
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
            if (FieldName is null)
            {
                return $"{TypeName}";
            }

            return $"{TypeName}.{FieldName}";
        }

        /// <summary>
        /// Converts a schema coordinate string into a <see cref="SchemaCoordinate"/> instance.
        /// </summary>
        /// <param name="s">
        /// The schema coordinate string.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="s"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The format of <paramref name="s"/> is wrong.
        /// </exception>
        public static implicit operator SchemaCoordinate(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            var parts = s.Split('.');

            if (parts.Length > 2)
            {
                throw new ArgumentException(
                    "The format for a schema coordinate is `TypeName.fieldName`",
                    nameof(s));
            }

            return parts.Length == 1
                ? new SchemaCoordinate(parts[0])
                : new SchemaCoordinate(parts[0], parts[1]);
        }
    }
}

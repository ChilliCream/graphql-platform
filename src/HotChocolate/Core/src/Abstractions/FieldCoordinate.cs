using System;

namespace HotChocolate
{
    /// <summary>
    /// A field in graphql is uniquely located within a parent type and hence code elements
    /// need to be specified using those coordinates.
    /// </summary>
    public readonly struct FieldCoordinate : IEquatable<FieldCoordinate>
    {
        public FieldCoordinate(
            NameString typeName,
            NameString fieldName,
            NameString? argumentName = null)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            FieldName = fieldName.EnsureNotEmpty(nameof(fieldName));
            ArgumentName = argumentName?.EnsureNotEmpty(nameof(argumentName));
            HasValue = true;
        }

        public void Deconstruct(
            out NameString typeName,
            out NameString fieldName,
            out NameString? argumentName)
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
            NameString fieldName,
            NameString? argumentName = null)
        {
            return new FieldCoordinate("__Empty", fieldName, argumentName);
        }

        /// <summary>
        /// Defines if this field coordinate is empty.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the type name to which this field coordinate is referring to.
        /// </summary>
        public NameString TypeName { get; }

        /// <summary>
        /// Gets the field name to which this field coordinate is referring to.
        /// </summary>
        public NameString FieldName { get; }

        /// <summary>
        /// Gets the argument name to which this field coordinate is referring to.
        /// Note: the argument name can be null if the coordinate is just referring to a field.
        /// </summary>
        public NameString? ArgumentName { get; }

        /// <summary>
        /// Create a new field coordinate based on the current one.
        /// </summary>
        public FieldCoordinate With(
            Optional<NameString> typeName = default,
            Optional<NameString> fieldName = default,
            Optional<NameString?> argumentName = default)
        {
            return new(
                typeName.HasValue ? typeName.Value : TypeName,
                fieldName.HasValue ? fieldName.Value : FieldName,
                argumentName.HasValue ? argumentName.Value : ArgumentName);
        }

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
            return TypeName.Equals(other.TypeName) &&
                   FieldName.Equals(other.FieldName) &&
                   Nullable.Equals(ArgumentName, other.ArgumentName);
        }

        public override bool Equals(object obj)
        {
            return obj is FieldCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TypeName.GetHashCode();
                hashCode = (hashCode * 397) ^ FieldName.GetHashCode();
                hashCode = (hashCode * 397) ^ ArgumentName.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            if (ArgumentName is null)
            {
                return $"{TypeName}.{FieldName}";
            }

            return $"{TypeName}.{FieldName}({ArgumentName})";
        }
    }
}

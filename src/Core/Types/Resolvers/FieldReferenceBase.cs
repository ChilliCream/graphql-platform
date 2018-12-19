using System;

namespace HotChocolate.Resolvers
{
    public class FieldReferenceBase
        : IFieldReference
    {
        protected FieldReferenceBase(NameString typeName, NameString fieldName)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            FieldName = fieldName.EnsureNotEmpty(nameof(fieldName));
        }

        protected FieldReferenceBase(FieldReferenceBase fieldReference)
        {
            if (fieldReference == null)
            {
                throw new ArgumentNullException(nameof(fieldReference));
            }

            TypeName = fieldReference.TypeName;
            FieldName = fieldReference.FieldName;
        }

        public NameString TypeName { get; }

        public NameString FieldName { get; }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return IsReferenceEqualTo(obj)
                || IsEqualTo(obj as FieldReferenceBase);
        }

        protected bool IsEqualTo(FieldReferenceBase other)
        {
            if (other is null)
            {
                return false;
            }

            if (IsReferenceEqualTo(other))
            {
                return true;
            }

            return other.TypeName.Equals(TypeName, StringComparison.Ordinal)
                && other.FieldName.Equals(FieldName, StringComparison.Ordinal);
        }

        protected bool IsReferenceEqualTo<T>(T value) where T : class
            => ReferenceEquals(this, value);

        public override int GetHashCode()
        {
            unchecked
            {
                return (TypeName.GetHashCode() * 397)
                    ^ (FieldName.GetHashCode() * 17);
            }
        }

        public override string ToString()
        {
            return $"{TypeName}.{FieldName}";
        }
    }
}

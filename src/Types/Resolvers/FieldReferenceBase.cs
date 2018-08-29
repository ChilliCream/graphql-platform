using System;

namespace HotChocolate.Resolvers
{
    public class FieldReferenceBase
    {
        protected FieldReferenceBase(string typeName, string fieldName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            TypeName = typeName;
            FieldName = fieldName;
        }

        public string TypeName { get; }

        public string FieldName { get; }

        public override bool Equals(object obj)
        {
            return IsReferenceEqualTo(obj)
                || IsEqualTo(obj as FieldReferenceBase);
        }

        protected bool IsEqualTo(FieldReferenceBase other)
        {
            if (IsReferenceEqualTo(other))
            {
                return true;
            }

            return other.TypeName.Equals(TypeName)
                && other.FieldName.Equals(FieldName);
        }

        protected bool IsReferenceEqualTo<T>(T value)
            where T : class
        {
            if (value is null)
            {
                return false;
            }

            return ReferenceEquals(this, value);
        }

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

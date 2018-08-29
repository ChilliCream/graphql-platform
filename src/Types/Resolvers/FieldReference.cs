using System;

namespace HotChocolate.Resolvers
{
    public sealed class FieldReference
        : FieldReferenceBase
        , IEquatable<FieldReference>
    {
        public FieldReference(string typeName, string fieldName)
            : base(typeName, fieldName)
        {
        }

        public FieldReference WithTypeName(string typeName)
        {
            return new FieldReference(typeName, FieldName);
        }

        public FieldReference WithFieldName(string fieldName)
        {
            return new FieldReference(TypeName, fieldName);
        }

        public bool Equals(FieldReference other)
        {
            return base.IsEqualTo(other);
        }
    }
}

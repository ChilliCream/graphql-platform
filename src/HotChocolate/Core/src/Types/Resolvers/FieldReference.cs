using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    public sealed class FieldReference
        : FieldReferenceBase
        , IEquatable<FieldReference>
    {
        public FieldReference(NameString typeName, NameString fieldName)
            : base(typeName, fieldName)
        {
        }

        public FieldReference WithTypeName(NameString typeName)
        {
            if (string.Equals(TypeName, typeName, StringComparison.Ordinal))
            {
                return this;
            }

            return new FieldReference(typeName, FieldName);
        }

        public FieldReference WithFieldName(NameString fieldName)
        {
            if (string.Equals(FieldName, fieldName, StringComparison.Ordinal))
            {
                return this;
            }

            return new FieldReference(TypeName, fieldName);
        }

        public FieldMember WithMember(MemberInfo member) =>
            new(TypeName, FieldName, member);

        public FieldMember WithExpression(Expression expression) =>
            new(TypeName, FieldName, expression);

        public FieldResolver WithResolver(FieldResolverDelegate resolver) =>
            new(TypeName, FieldName, resolver);

        public bool Equals(FieldReference other) =>
            IsEqualTo(other);
    }
}

using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers;

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
        if (string.Equals(TypeName, typeName, StringComparison.Ordinal))
        {
            return this;
        }

        return new FieldReference(typeName, FieldName);
    }

    public FieldReference WithFieldName(string fieldName)
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

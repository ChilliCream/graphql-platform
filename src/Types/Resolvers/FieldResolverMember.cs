using System;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    public class FieldResolverMember
        : FieldReference
        , IEquatable<FieldResolverMember>
    {
        public FieldResolverMember(string typeName, string fieldName, MemberInfo member)
            : base(typeName, fieldName)
        {
            Member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public MemberInfo Member { get; }

        public FieldResolverMember WithTypeName(string typeName)
        {
            return new FieldResolverMember(typeName, FieldName, Member);
        }

        public FieldResolverMember WithFieldName(string fieldName)
        {
            return new FieldResolverMember(TypeName, fieldName, Member);
        }

        public bool Equals(FieldResolverMember other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.TypeName.Equals(TypeName)
                && other.FieldName.Equals(FieldName)
                && other.Member.Equals(Member);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397)
                    ^ (Member.GetHashCode() * 17);
            }
        }

        public override string ToString()
        {
            return $"{ToString()} => {Member.Name}";
        }
    }
}

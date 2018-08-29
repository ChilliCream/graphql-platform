using System;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    public sealed class FieldMember
        : FieldReferenceBase
        , IEquatable<FieldMember>
    {
        public FieldMember(string typeName, string fieldName, MemberInfo member)
            : base(typeName, fieldName)
        {
            Member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public MemberInfo Member { get; }

        public FieldMember WithTypeName(string typeName)
        {
            return new FieldMember(typeName, FieldName, Member);
        }

        public FieldMember WithFieldName(string fieldName)
        {
            return new FieldMember(TypeName, fieldName, Member);
        }

        public FieldMember WithMember(MemberInfo member)
        {
            return new FieldMember(TypeName, FieldName, member);
        }

        public bool Equals(FieldMember other)
        {
            return IsEqualTo(other);
        }

        public override bool Equals(object obj)
        {
            if (IsReferenceEqualTo(obj))
            {
                return true;
            }

            return IsEqualTo(obj as FieldMember);
        }

        private bool IsEqualTo(FieldMember other)
        {
            return base.IsEqualTo(other)
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
            return $"{base.ToString()} => {Member.Name}";
        }
    }
}

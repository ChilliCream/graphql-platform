using System;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    public sealed class FieldMember
        : FieldReferenceBase
        , IEquatable<FieldMember>
    {
        private FieldReference _fieldReference;

        public FieldMember(
            NameString typeName,
            NameString fieldName,
            MemberInfo member)
            : base(typeName, fieldName)
        {
            Member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public FieldMember(FieldReference fieldReference, MemberInfo member)
            : base(fieldReference)
        {
            _fieldReference = fieldReference;
            Member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public MemberInfo Member { get; }

        public FieldMember WithTypeName(NameString typeName)
        {
            if (string.Equals(TypeName, typeName, StringComparison.Ordinal))
            {
                return this;
            }

            return new FieldMember(typeName, FieldName, Member);
        }

        public FieldMember WithFieldName(NameString fieldName)
        {
            if (string.Equals(FieldName, fieldName, StringComparison.Ordinal))
            {
                return this;
            }

            return new FieldMember(TypeName, fieldName, Member);
        }

        public FieldMember WithMember(MemberInfo member)
        {
            if (Equals(Member, member))
            {
                return this;
            }

            return new FieldMember(TypeName, FieldName, member);
        }

        public bool Equals(FieldMember other)
        {
            return IsEqualTo(other);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (IsReferenceEqualTo(obj))
            {
                return true;
            }

            return IsEqualTo(obj as FieldMember);
        }

        private bool IsEqualTo(FieldMember other)
        {
            if (other is null)
            {
                return false;
            }

            if (IsReferenceEqualTo(other))
            {
                return true;
            }

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

        public FieldReference ToFieldReference()
        {
            if (_fieldReference == null)
            {
                _fieldReference = new FieldReference(TypeName, FieldName);
            }

            return _fieldReference;
        }
    }
}

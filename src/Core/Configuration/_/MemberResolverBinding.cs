using System;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Configuration
{
    public class MemberResolverBinding
        : ResolverBinding
    {
        public MemberResolverBinding(
            string typeName,
            string fieldName,
            MemberInfo fieldMember)
            : base(typeName, fieldName)
        {
            if (fieldMember == null)
            {
                throw new ArgumentNullException(nameof(fieldMember));
            }

            FieldMember = fieldMember;
        }

        public MemberInfo FieldMember { get; }
    }
}

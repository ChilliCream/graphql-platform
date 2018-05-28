using System;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate
{
    public class MemberResolverBinding
        : ResolverBinding
    {
        public MemberResolverBinding(
            string typeName,
            string fieldName,
            MemberInfo memberInfo)
            : base(typeName)
        {
            if (fieldName == null)
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(typeName));
            }

            if (ValidationHelper.IsTypeNameValid(fieldName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(typeName));
            }
        }

        public string FieldName { get; set; }
        public MemberInfo FieldMember { get; set; }
    }
}

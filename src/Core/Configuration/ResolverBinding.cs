using System;
using HotChocolate.Internal;

namespace HotChocolate.Configuration
{
    internal abstract class ResolverBinding
    {
        public ResolverBinding(string typeName, string fieldName)
        {
            if (typeName == null)
            {
                throw new ArgumentException(
                    "The type name cannot be null or empty.",
                    nameof(typeName));
            }

            if (!ValidationHelper.IsTypeNameValid(typeName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(typeName));
            }

            if (!ValidationHelper.IsTypeNameValid(fieldName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(typeName));
            }

            TypeName = typeName;
            FieldName = fieldName;
        }

        public string TypeName { get; }
        public string FieldName { get; }
    }
}

using System;
using HotChocolate.Internal;

namespace HotChocolate
{
    public abstract class ResolverBinding
    {
        public ResolverBinding(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentException(
                    "The type name cannot be null or empty.",
                    nameof(typeName));
            }

            if (ValidationHelper.IsTypeNameValid(typeName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(typeName));
            }

            TypeName = typeName;
        }

        public string TypeName { get; }
    }
}

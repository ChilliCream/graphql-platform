using System;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    internal sealed class FieldResolver
    {
        public FieldResolver(
            string typeName, string fieldName,
            FieldResolverDelegate resolver)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            TypeName = typeName;
            FieldName = fieldName;
            Resolver = resolver;
        }

        public string TypeName { get; }
        public string FieldName { get; }
        public FieldResolverDelegate Resolver { get; }
    }
}
using System;

namespace HotChocolate.Resolvers
{
    public sealed class FieldResolver
        : FieldReference
    {
        public FieldResolver(
            string typeName, string fieldName,
            FieldResolverDelegate resolver)
            : base(typeName, fieldName)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            Resolver = resolver;
        }

        public FieldResolverDelegate Resolver { get; }
    }
}
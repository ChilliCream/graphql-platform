using System;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class DelegateResolverBinding
        : ResolverBinding
    {
        public DelegateResolverBinding(
            string typeName,
            string fieldName,
            FieldResolverDelegate fieldResolver)
            : base(typeName, fieldName)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            FieldResolver = fieldResolver;
        }

        public FieldResolverDelegate FieldResolver { get; }
    }
}

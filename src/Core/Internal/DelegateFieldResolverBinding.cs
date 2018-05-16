using System;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    internal class DelegateFieldResolverBinding
        : IFieldResolverBinding
    {
        public DelegateFieldResolverBinding(
            string typeName, string fieldName,
            FieldResolverDelegate resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            FieldName = fieldName;
            Resolver = resolver;
        }

        public DelegateFieldResolverBinding(
            string typeName, string fieldName,
            AsyncFieldResolverDelegate fieldResolver)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            FieldName = fieldName;
            Resolver = new FieldResolverDelegate(
                (ctx, ct) => fieldResolver(ctx, ct));
        }

        public string TypeName { get; }
        public string FieldName { get; }
        public FieldResolverDelegate Resolver { get; }
    }


}

using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class ResolverDelegateBindingHandler
        : IResolverBindingHandler
    {
        public void ApplyBinding(
            ISchemaContext schemaContext,
            ResolverBindingInfo resolverBindingInfo)
        {
            if (resolverBindingInfo is ResolverDelegateBindingInfo b)
            {
                schemaContext.Resolvers.RegisterResolver(
                    b.CreateFieldResolver());
            }
            else
            {
                throw new NotSupportedException(
                    "The binding type is not supported by this handler.");
            }
        }
    }
}

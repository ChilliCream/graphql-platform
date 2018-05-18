using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class ResolverDelegateBindingHandler
        : IResolverBindingHandler
    {
        public IEnumerable<Resolvers.FieldResolver> ApplyBinding(
            ResolverBindingInfo resolverBindingInfo)
        {
            if (resolverBindingInfo is ResolverDelegateBindingInfo b)
            {
                if (b.AsyncFieldResolver == null)
                {
                    yield return new Resolvers.FieldResolver(
                        b.ObjectTypeName, b.FieldName, b.FieldResolver);
                }
                else
                {
                    FieldResolverDelegate fieldResolverDelegate =
                        (ctx, ct) => b.AsyncFieldResolver(ctx, ct);
                    yield return new Resolvers.FieldResolver(
                        b.ObjectTypeName, b.FieldName, fieldResolverDelegate);
                }
            }
        }
    }

}

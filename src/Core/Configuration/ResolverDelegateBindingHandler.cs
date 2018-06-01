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
                if (b.AsyncFieldResolver == null)
                {
                    schemaContext.Resolvers.RegisterResolver(
                        new DelegateResolverBinding(
                            b.ObjectTypeName, b.FieldName, b.FieldResolver));
                }
                else
                {
                    FieldResolverDelegate fieldResolverDelegate =
                        (ctx, ct) => b.AsyncFieldResolver(ctx, ct);
                    schemaContext.Resolvers.RegisterResolver(
                        new DelegateResolverBinding(
                            b.ObjectTypeName, b.FieldName, fieldResolverDelegate));
                }
            }
        }
    }
}

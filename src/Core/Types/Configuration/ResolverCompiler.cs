using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Configuration
{
    internal static class ResolverCompiler
    {
        public static void Compile(
            IDictionary<FieldReference, RegisteredResolver> resolvers)
        {
            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            var resolverBuilder = new ResolverBuilder();

            foreach (RegisteredResolver resolver in resolvers.Values)
            {
                if (resolver.Field is FieldMember member)
                {
                    if (resolver.IsSourceResolver)
                    {
                        resolverBuilder.AddDescriptor(
                            new SourceResolverDescriptor(
                                resolver.SourceType, member));
                    }
                    else
                    {
                        resolverBuilder.AddDescriptor(
                            new ResolverDescriptor(
                                resolver.ResolverType,
                                resolver.ResolverType,
                                member));
                    }
                }
            }


            ResolverBuilderResult result = resolverBuilder.Build();

            foreach (FieldResolver resolver in result.Resolvers)
            {
                var reference = resolver.ToFieldReference();
                if (resolvers.TryGetValue(reference,
                    out RegisteredResolver registered))
                {
                    resolvers[reference] = registered.WithField(resolver);
                }
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate
{
    internal sealed class ResolverCompiler
    {
        private Dictionary<FieldReference, RegisteredResolver> Resolvers
        { get; } = new Dictionary<FieldReference, RegisteredResolver>();

        public void AddResolvers(
            IDictionary<FieldReference, RegisteredResolver> resolvers)
        {
            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }
        }

        public IDictionary<FieldReference, RegisteredResolver> Compile()
        {
            var resolverBuilder = new ResolverBuilder();

            foreach (RegisteredResolver resolver in Resolvers.Values.ToList())
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
                FieldReference reference = resolver.ToFieldReference();
                if (Resolvers.TryGetValue(reference,
                    out RegisteredResolver registered))
                {
                    Resolvers[reference] = new RegisteredResolver(
                        registered.ResolverType,
                        registered.SourceType,
                        resolver);
                }
            }

            return Resolvers;
        }
    }
}

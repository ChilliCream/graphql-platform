using System;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation
{
    public class EntityResolverDefinition: DefinitionBase
    {
        public Type? ResolvedEntityType { get; set; }

        public FieldResolverDelegate? Resolver { get; set; }
    }
}

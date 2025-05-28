using HotChocolate.Resolvers;

namespace HotChocolate.ApolloFederation;

internal sealed record ReferenceResolver(FieldResolverDelegate Resolver);

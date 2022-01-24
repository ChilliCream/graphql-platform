using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.ApolloFederation;

public readonly struct ReferenceResolverDefinition
{
    public ReferenceResolverDefinition(
        FieldResolverDelegate resolver, 
        IReadOnlyList<string[]>? required = default)
    {
        Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        Required = required ?? Array.Empty<string[]>();
    }

    public FieldResolverDelegate Resolver { get; }

    public IReadOnlyList<string[]> Required { get; }
}

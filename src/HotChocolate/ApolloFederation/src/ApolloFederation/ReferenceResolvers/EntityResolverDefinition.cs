using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation;

public sealed class EntityResolverDefinition : DefinitionBase
{
    public Type? ResolvedEntityType { get; set; }

    public ReferenceResolverDefinition? ResolverDefinition { get; set; }
}

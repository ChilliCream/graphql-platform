using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// The entity definition allows to specify a reference resolver.
/// </summary>
public sealed class EntityResolverDefinition : DefinitionBase
{
    /// <summary>
    /// The runtime type of the entity.
    /// </summary>
    public Type? EntityType { get; set; }

    /// <summary>
    /// The reference resolver definition.
    /// </summary>
    public ReferenceResolverDefinition? ResolverDefinition { get; set; }
}

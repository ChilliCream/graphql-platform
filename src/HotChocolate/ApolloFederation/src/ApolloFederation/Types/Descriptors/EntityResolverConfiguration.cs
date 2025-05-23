using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// The entity definition allows specifying a reference resolver.
/// </summary>
public sealed class EntityResolverConfiguration : TypeSystemConfiguration
{
    /// <summary>
    /// The runtime type of the entity.
    /// </summary>
    public Type? EntityType { get; set; }

    /// <summary>
    /// The reference resolver definition.
    /// </summary>
    public ReferenceResolverConfiguration? Resolver { get; set; }
}

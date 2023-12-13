namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Defines the contract for enriching a group of entities with additional metadata and
/// functionality.
/// </summary>
internal interface IEntityEnricher
{
    /// <summary>
    /// Enriches the entity group with additional metadata and functionality.
    /// </summary>
    /// <param name="context">The composition context.</param>
    /// <param name="entity">The entity group.</param>
    void Enrich(CompositionContext context, EntityGroup entity);
}

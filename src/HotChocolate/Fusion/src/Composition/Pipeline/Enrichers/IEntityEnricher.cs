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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default);
}

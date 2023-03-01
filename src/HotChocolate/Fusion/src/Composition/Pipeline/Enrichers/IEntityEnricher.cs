namespace HotChocolate.Fusion.Composition.Pipeline;

public interface IEntityEnricher
{
    ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default);
}

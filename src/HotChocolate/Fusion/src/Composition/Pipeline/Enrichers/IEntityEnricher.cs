namespace HotChocolate.Fusion.Composition;

public interface IEntityEnricher
{
    ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default);
}

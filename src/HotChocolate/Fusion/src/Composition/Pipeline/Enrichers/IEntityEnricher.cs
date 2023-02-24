using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public interface IEntityEnricher
{
    ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup typeGroup,
        CancellationToken cancellationToken = default);
}

public sealed record EntityGroup(
    string Name,
    IReadOnlyList<EntityPart> Parts)
{
    public EntityMetadata Metadata { get; } = new();
}

public sealed record EntityPart(ObjectType Type, Schema Schema);

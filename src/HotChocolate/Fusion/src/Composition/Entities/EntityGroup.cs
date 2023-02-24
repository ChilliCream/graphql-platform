namespace HotChocolate.Fusion.Composition;

public sealed record EntityGroup(
    string Name,
    IReadOnlyList<EntityPart> Parts)
{
    public EntityMetadata Metadata { get; } = new();
}

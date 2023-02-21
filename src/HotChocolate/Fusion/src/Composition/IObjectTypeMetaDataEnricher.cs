using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public interface IObjectTypeMetaDataEnricher
{
    ValueTask EnrichAsync(ObjectTypeGroup typeGroup, CancellationToken cancellationToken = default);
}

public sealed record ObjectTypeGroup(
    string Name,
    IReadOnlyList<ObjectTypeInfo> Types)
{
    public ObjectTypeMetadata Metadata { get; } = new();
}

public sealed record ObjectTypeInfo(ObjectType Type, Schema Schema);

namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphB;

public sealed class Entity
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}

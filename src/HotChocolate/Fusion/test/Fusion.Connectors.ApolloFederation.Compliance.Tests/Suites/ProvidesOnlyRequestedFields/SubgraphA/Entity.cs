namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphA;

public sealed class Entity
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string Extra { get; init; }
}

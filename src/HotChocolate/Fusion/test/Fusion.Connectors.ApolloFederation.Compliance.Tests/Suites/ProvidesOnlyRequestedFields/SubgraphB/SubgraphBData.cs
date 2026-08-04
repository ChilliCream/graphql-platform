namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphB;

internal static class SubgraphBData
{
    public static readonly IReadOnlyList<Entity> Entities =
    [
        new Entity
        {
            Id = "e1",
            Name = "Entity One",
            Description = "Description One"
        },
        new Entity
        {
            Id = "e2",
            Name = "Entity Two",
            Description = "Description Two"
        }
    ];
}

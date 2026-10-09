namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Seed data for subgraph <c>b</c> of the <c>requires-with-fragments</c> suite.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<Entity> Entities =
    [
        new Entity { Id = "e1" },
        new Entity { Id = "e2" }
    ];

    public static readonly IReadOnlyDictionary<string, Entity> EntitiesById =
        Entities.ToDictionary(static e => e.Id, StringComparer.Ordinal);
}

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// Seed data for subgraph <c>a</c> of the <c>requires-with-fragments</c> suite.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<Entity> Entities =
    [
        new Entity { Id = "e1", DataId = "b1" },
        new Entity { Id = "e2", DataId = "q1" }
    ];

    public static readonly IReadOnlyDictionary<string, Entity> EntitiesById =
        Entities.ToDictionary(static e => e.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<Baz> Bazs =
    [
        new Baz { Foo = "b1-foo", Bar = "b1-bar", BazValue = "b1-baz" }
    ];

    public static readonly IReadOnlyDictionary<string, Baz> BazsById =
        new Dictionary<string, Baz>(StringComparer.Ordinal)
        {
            ["b1"] = Bazs[0]
        };

    public static readonly IReadOnlyList<Qux> Quxs =
    [
        new Qux { Foo = "q1-foo", Bar = "q1-bar", QuxValue = "q1-qux" }
    ];

    public static readonly IReadOnlyDictionary<string, Qux> QuxsById =
        new Dictionary<string, Qux>(StringComparer.Ordinal)
        {
            ["q1"] = Quxs[0]
        };

    public static object? ResolveData(string dataId)
    {
        if (BazsById.TryGetValue(dataId, out var baz))
        {
            return baz;
        }

        if (QuxsById.TryGetValue(dataId, out var qux))
        {
            return qux;
        }

        return null;
    }
}

namespace HotChocolate.Fusion.Suites.InputObjectIntersection.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Name = "u1-name" },
        new User { Id = "u2", Name = "u2-name" }
    ];

    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);
}

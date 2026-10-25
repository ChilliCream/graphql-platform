namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/interface-object-with-requires/data.ts</c>.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Name = "u1-name", Age = 11 },
        new User { Id = "u2", Name = "u2-name", Age = 22 }
    ];

    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);
}

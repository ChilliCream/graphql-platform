namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph. Subgraph <c>a</c> can only
/// surface the <c>REGULAR</c> enum value because it never declared
/// <c>ANONYMOUS</c>; <c>u2</c> therefore reaches the gateway with
/// <c>type = null</c>.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Type = UserTypeEnum.REGULAR },
        new User { Id = "u2", Type = null }
    ];

    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id!, StringComparer.Ordinal);
}

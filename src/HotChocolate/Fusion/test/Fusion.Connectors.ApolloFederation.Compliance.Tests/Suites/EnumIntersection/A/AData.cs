namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph. The audit data source stores
/// <c>ANONYMOUS</c> for <c>u2</c>, but subgraph <c>a</c> never exposes that
/// value in its GraphQL enum, so serializing it fails at the subgraph and the
/// gateway surfaces the error with <c>type = null</c>.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Type = UserTypeEnum.REGULAR },
        new User { Id = "u2", Type = UserTypeEnum.ANONYMOUS }
    ];

    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id!, StringComparer.Ordinal);
}

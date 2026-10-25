namespace HotChocolate.Fusion.Suites.EnumIntersection.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph. Both <c>REGULAR</c> and
/// <c>ANONYMOUS</c> values are present in the source-side enum here.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Type = UserTypeEnum.REGULAR },
        new User { Id = "u2", Type = UserTypeEnum.ANONYMOUS }
    ];

    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id!, StringComparer.Ordinal);
}

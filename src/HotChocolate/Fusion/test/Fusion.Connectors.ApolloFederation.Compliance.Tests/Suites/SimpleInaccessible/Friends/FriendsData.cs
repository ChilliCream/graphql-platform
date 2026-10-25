namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

/// <summary>
/// Seed data for the <c>friends</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-inaccessible/data.ts</c>.
/// </summary>
internal static class FriendsData
{
    /// <summary>
    /// The seeded users, ordered by id.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1" },
        new User { Id = "u2" }
    ];

    /// <summary>
    /// Friend-id lookup keyed by user id.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> FriendsByUserId =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["u1"] = ["u2"],
            ["u2"] = ["u1"]
        };

    /// <summary>
    /// Lookup of seeded users keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id!, StringComparer.Ordinal);
}

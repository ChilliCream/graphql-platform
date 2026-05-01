namespace HotChocolate.Fusion.Suites.Typename.Shared;

/// <summary>
/// Seed data for the <c>typename</c> suite, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/typename/data.ts</c>.
/// Both subgraphs (<c>a</c>, <c>b</c>) read from this list when resolving
/// <c>User</c> entity references and the <c>users</c> root field.
/// </summary>
public static class TypenameData
{
    /// <summary>
    /// The seeded user rows. Each carries the concrete type name from the
    /// audit fixture so subgraph <c>a</c>'s <c>__resolveReference</c> can
    /// dispatch to the right concrete implementer.
    /// </summary>
    public static readonly IReadOnlyList<UserRow> Users =
    [
        new UserRow("Admin", "u1", "Alice", IsMain: false),
        new UserRow("Admin", "u2", "Bob", IsMain: true)
    ];

    /// <summary>
    /// Looks up a user row by id, returning <c>null</c> when absent.
    /// </summary>
    public static UserRow? FindUser(string id)
    {
        foreach (var user in Users)
        {
            if (string.Equals(user.Id, id, StringComparison.Ordinal))
            {
                return user;
            }
        }

        return null;
    }
}

/// <summary>
/// Source row for a user in the <c>typename</c> seed data. The
/// <see cref="Typename"/> field captures the audit's <c>__typename</c>
/// hint that tags every row with its concrete implementer.
/// </summary>
public sealed record UserRow(string Typename, string Id, string Name, bool IsMain);

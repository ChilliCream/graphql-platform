namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-interface-object/data.ts</c>.
/// Subgraph <c>b</c> contributes <c>username</c> to <c>NodeWithName</c> and
/// <c>name</c> to <c>Account</c> via <c>@interfaceObject</c>.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<NodeWithName> Users =
    [
        new NodeWithName { Id = "u1", Username = "u1-username" },
        new NodeWithName { Id = "u2", Username = "u2-username" }
    ];

    public static readonly IReadOnlyList<Account> Accounts =
    [
        new Account { Id = "u1", Name = "Alice" },
        new Account { Id = "u2", Name = "Bob" },
        new Account { Id = "u3", Name = "Charlie" }
    ];

    public static string? FindUsername(string id)
    {
        foreach (var user in Users)
        {
            if (string.Equals(user.Id, id, StringComparison.Ordinal))
            {
                return user.Username;
            }
        }

        return null;
    }

    public static string? FindName(string id)
    {
        foreach (var account in Accounts)
        {
            if (string.Equals(account.Id, id, StringComparison.Ordinal))
            {
                return account.Name;
            }
        }

        return null;
    }
}

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-interface-object/data.ts</c>.
/// Subgraph <c>a</c> owns the <c>User</c> entity and the concrete
/// <c>Admin</c> / <c>Regular</c> implementers of the <c>Account</c> interface.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Name = "u1-name", Age = 11 },
        new User { Id = "u2", Name = "u2-name", Age = 22 }
    ];

    public static readonly IReadOnlyList<AccountRow> Accounts =
    [
        new AccountRow("Admin", "u1", IsMain: false, IsActive: true),
        new AccountRow("Admin", "u2", IsMain: true, IsActive: true),
        new AccountRow("Regular", "u3", IsMain: false, IsActive: true)
    ];

    public static User? FindUser(string id)
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

    public static AccountRow? FindAccount(string id)
    {
        foreach (var account in Accounts)
        {
            if (string.Equals(account.Id, id, StringComparison.Ordinal))
            {
                return account;
            }
        }

        return null;
    }

    public static IAccount? ResolveAccount(string id)
    {
        var row = FindAccount(id);

        return row switch
        {
            null => null,
            { Typename: "Regular" } => new Regular { Id = row.Id, IsMain = row.IsMain },
            _ => new Admin { Id = row.Id, IsMain = row.IsMain, IsActive = row.IsActive }
        };
    }
}

/// <summary>
/// Source row for an account in the <c>a</c> seed data. <see cref="Typename"/>
/// tags every row with its concrete implementer (<c>Admin</c> or
/// <c>Regular</c>) so the <c>Account</c> reference resolver can dispatch to the
/// correct concrete type.
/// </summary>
internal sealed record AccountRow(string Typename, string Id, bool IsMain, bool IsActive);

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

/// <summary>
/// Seed data for subgraph <c>b</c>.
/// </summary>
internal static class SubgraphBData
{
    public static readonly IReadOnlyList<Account> Accounts =
    [
        new Account { Id = "a1" }
    ];

    public static readonly IReadOnlyDictionary<string, Account> AccountsById =
        Accounts.ToDictionary(static a => a.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<Chat> Chats =
    [
        new Chat { Id = "c1", AccountId = "a1", Text = "c1-text" }
    ];

    public static readonly IReadOnlyDictionary<string, Chat> ChatsById =
        Chats.ToDictionary(static c => c.Id, StringComparer.Ordinal);
}

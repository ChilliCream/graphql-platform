namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// Seed data for subgraph <c>a</c> of the <c>requires-interface</c> suite.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<(string Id, string Name, string AddressId)> Users =
    [
        ("u1", "u1-name", "a1"),
        ("u2", "u2-name", "a2")
    ];

    public static readonly IReadOnlyDictionary<string, (string Id, string Name, string AddressId)> UsersById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<IAddress> Addresses =
    [
        new HomeAddress { Id = "a1", City = "a1-city", Country = "a1-country" },
        new WorkAddress { Id = "a2", City = "a2-city", Country = "a2-country" }
    ];

    public static readonly IReadOnlyDictionary<string, IAddress> AddressesById =
        Addresses.ToDictionary(static a => a.Id, StringComparer.Ordinal);
}

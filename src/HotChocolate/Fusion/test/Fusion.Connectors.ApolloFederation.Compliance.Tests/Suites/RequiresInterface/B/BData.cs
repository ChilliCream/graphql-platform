namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Seed data for subgraph <c>b</c> of the <c>requires-interface</c> suite.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Name = "u1-name", AddressId = "a1" },
        new User { Id = "u2", Name = "u2-name", AddressId = "a2" }
    ];

    public static readonly IReadOnlyDictionary<string, User> UsersById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<IAddress> Addresses =
    [
        new HomeAddress { Id = "a1", City = "a1-city" },
        new WorkAddress { Id = "a2", City = "a2-city" }
    ];

    public static readonly IReadOnlyDictionary<string, IAddress> AddressesById =
        Addresses.ToDictionary(static a => a.Id, StringComparer.Ordinal);
}

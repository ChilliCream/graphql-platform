using HotChocolate.Fusion.Suites.AbstractTypes.Products;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Users;

public static class UserData
{
    public static readonly IReadOnlyList<User> Users =
    [
        new User { InternalId = "u1", Email = "u1@example.com", Name = "u1-name" },
        new User { InternalId = "u2", Email = "u2@example.com", Name = "u2-name" },
        new User { InternalId = "u3", Email = "u3@example.com", Name = "u3-name" }
    ];

    public static readonly IReadOnlyDictionary<string, User> UsersByEmail =
        Users.ToDictionary(static u => u.Email, StringComparer.Ordinal);

    public static int CountProductsCreated(string internalId)
        => ProductData.AllProducts.Count(p => p.CreatedByInternalId == internalId);
}

public sealed class User
{
    public string InternalId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string? Name { get; init; }
}

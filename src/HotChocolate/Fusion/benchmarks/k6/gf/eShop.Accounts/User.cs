namespace eShop.Accounts;

public sealed class User
{
    public required string Id { get; init; }

    public string? Name { get; init; }

    public string? Username { get; init; }

    public int? Birthday { get; init; }
}

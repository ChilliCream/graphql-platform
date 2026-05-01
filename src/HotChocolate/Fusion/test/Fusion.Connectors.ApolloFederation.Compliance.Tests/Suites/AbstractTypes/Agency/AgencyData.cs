namespace HotChocolate.Fusion.Suites.AbstractTypes.Agency;

public static class AgencyData
{
    public static readonly IReadOnlyList<AgencyEntity> Agencies =
    [
        new AgencyEntity
        {
            Id = "a1",
            CompanyName = "Agency 1",
            Email = new EmailValue { Address = "a1@example.com" }
        }
    ];

    public static readonly IReadOnlyDictionary<string, AgencyEntity> AgenciesById =
        Agencies.ToDictionary(static a => a.Id, StringComparer.Ordinal);

    public static GroupEntity ResolveGroup(string id)
        => new()
        {
            Id = id,
            Name = "Group " + id,
            Email = "group" + id + "@example.com"
        };
}

public sealed class AgencyEntity
{
    public string Id { get; init; } = default!;
    public string? CompanyName { get; init; }
    public EmailValue? Email { get; init; }
}

public sealed class EmailValue
{
    public string? Address { get; init; }
}

public sealed class GroupEntity
{
    public string Id { get; init; } = default!;
    public string? Name { get; init; }
    public string? Email { get; init; }
}

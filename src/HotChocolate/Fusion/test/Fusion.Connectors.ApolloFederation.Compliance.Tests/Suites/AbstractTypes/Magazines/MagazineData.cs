namespace HotChocolate.Fusion.Suites.AbstractTypes.Magazines;

public static class MagazineData
{
    public static readonly IReadOnlyList<Magazine> Magazines =
    [
        new Magazine { Id = "p2", Title = "Magazine 1" },
        new Magazine { Id = "p4", Title = "Magazine 2" }
    ];

    public static readonly IReadOnlyDictionary<string, Magazine> MagazinesById =
        Magazines.ToDictionary(static m => m.Id, StringComparer.Ordinal);
}

public sealed class Magazine
{
    public string Id { get; init; } = default!;
    public string? Title { get; init; }
}

namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// Seed data for authors in subgraph <c>a</c>.
/// </summary>
internal static class AuthorData
{
    public static readonly IReadOnlyList<Author> Authors =
    [
        new Author { Id = "a1", Name = "John", YearsOfExperience = 5 },
        new Author { Id = "a2", Name = "Jane", YearsOfExperience = 20 }
    ];

    public static readonly IReadOnlyDictionary<string, Author> ById =
        Authors.ToDictionary(static a => a.Id, StringComparer.Ordinal);
}

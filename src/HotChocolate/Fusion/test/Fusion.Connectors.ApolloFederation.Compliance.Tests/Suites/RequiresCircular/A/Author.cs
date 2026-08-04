namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// The <c>Author</c> entity in subgraph <c>a</c>.
/// Owns all author fields.
/// </summary>
public sealed class Author
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;

    public int YearsOfExperience { get; init; }
}

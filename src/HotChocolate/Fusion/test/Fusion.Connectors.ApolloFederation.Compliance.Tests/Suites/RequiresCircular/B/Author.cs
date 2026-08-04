namespace HotChocolate.Fusion.Suites.RequiresCircular.B;

/// <summary>
/// The <c>Author</c> entity in subgraph <c>b</c>.
/// <c>yearsOfExperience</c> is external in this subgraph.
/// </summary>
public sealed class Author
{
    public string Id { get; set; } = default!;

    public int? YearsOfExperience { get; set; }
}

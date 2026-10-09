namespace HotChocolate.Fusion.Suites.RequiresCircular.B;

/// <summary>
/// The <c>Post</c> entity in subgraph <c>b</c>.
/// Owns <c>author</c> and computes <c>byNovice</c> via
/// <c>@requires(fields: "author { yearsOfExperience }")</c>.
/// </summary>
public sealed class Post
{
    public string Id { get; set; } = default!;

    public Author? Author { get; set; }
}

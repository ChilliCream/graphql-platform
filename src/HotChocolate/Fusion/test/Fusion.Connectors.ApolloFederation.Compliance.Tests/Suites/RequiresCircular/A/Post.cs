namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// The <c>Post</c> entity in subgraph <c>a</c>.
/// <c>byNovice</c> is external, populated by the gateway when projecting
/// <c>@requires</c> dependencies for <c>byExpert</c>.
/// </summary>
public sealed class Post
{
    public string Id { get; set; } = default!;

    public bool? ByNovice { get; set; }
}

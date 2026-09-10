namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public sealed class ReviewResult
{
    public int Id { get; init; }
    public string Body { get; init; } = default!;
    public object? Product { get; init; }
}

public sealed class ReviewBookEntity
{
    public string Id { get; init; } = default!;
    public IReadOnlyList<BookSimilarRef>? Similar { get; init; }
}

public sealed class ReviewMagazineEntity
{
    public string Id { get; init; } = default!;
    public IReadOnlyList<MagazineSimilarRef>? Similar { get; init; }
}

public sealed class BookSimilarRef
{
    public string Id { get; init; } = default!;
}

public sealed class MagazineSimilarRef
{
    public string Id { get; init; } = default!;
}

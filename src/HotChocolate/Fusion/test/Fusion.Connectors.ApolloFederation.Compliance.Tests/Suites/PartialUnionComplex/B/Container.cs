namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

public sealed class Container
{
    public string Id { get; init; } = default!;

    public IReadOnlyList<object> Actions { get; init; } = [];
}

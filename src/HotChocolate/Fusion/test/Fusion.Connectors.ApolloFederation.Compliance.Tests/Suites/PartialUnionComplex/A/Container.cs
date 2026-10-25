namespace HotChocolate.Fusion.Suites.PartialUnionComplex.A;

public sealed class Container
{
    public string Id { get; init; } = default!;

    public IReadOnlyList<object> Actions { get; init; } = [];
}

namespace HotChocolate.Fusion.Suites.PartialUnion.A;

public sealed class Response
{
    public IReadOnlyList<object> Actions { get; init; } = [];
    public string? Message { get; init; }
}

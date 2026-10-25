namespace HotChocolate.Fusion.Suites.PartialUnion.B;

public sealed class Response
{
    public IReadOnlyList<object> Actions { get; init; } = [];
    public string? Message { get; init; }
}

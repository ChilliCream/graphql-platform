namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// The <c>B</c> entity in the <c>b</c> subgraph (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class B
{
    public string Id { get; init; } = default!;

    public IReadOnlyList<A> A { get; init; } = [];
}

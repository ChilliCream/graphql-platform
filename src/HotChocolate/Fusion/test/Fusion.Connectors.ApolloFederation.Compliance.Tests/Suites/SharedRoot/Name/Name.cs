namespace HotChocolate.Fusion.Suites.SharedRoot.Name;

/// <summary>
/// The <c>Name</c> value type owned by the <c>name</c> subgraph.
/// </summary>
public sealed class Name
{
    public string Id { get; init; } = default!;

    public string Brand { get; init; } = default!;

    public string Model { get; init; } = default!;
}

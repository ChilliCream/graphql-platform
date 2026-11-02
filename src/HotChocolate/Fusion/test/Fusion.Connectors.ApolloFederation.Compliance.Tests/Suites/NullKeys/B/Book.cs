namespace HotChocolate.Fusion.Suites.NullKeys.B;

/// <summary>
/// The <c>Book</c> entity as projected by the <c>b</c> subgraph
/// (<c>@key(fields: "id")</c> and <c>@key(fields: "upc")</c>).
/// </summary>
public sealed class Book
{
    public string Id { get; init; } = default!;

    public string Upc { get; init; } = default!;
}

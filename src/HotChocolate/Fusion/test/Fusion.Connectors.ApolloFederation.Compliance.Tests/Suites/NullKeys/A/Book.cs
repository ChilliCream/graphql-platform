namespace HotChocolate.Fusion.Suites.NullKeys.A;

/// <summary>
/// The <c>Book</c> entity as projected by the <c>a</c> subgraph
/// (<c>@key(fields: "upc")</c>).
/// </summary>
public sealed class Book
{
    public string Upc { get; init; } = default!;
}

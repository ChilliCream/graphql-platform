namespace HotChocolate.Fusion.Suites.NullKeys.A;

/// <summary>
/// The <c>BookContainer</c> wrapper exposed by the <c>a</c> subgraph.
/// </summary>
public sealed class BookContainer
{
    public Book? Book { get; init; }
}

namespace HotChocolate.Fusion.Suites.KeysMashup.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<A> Items =
    [
        new A
        {
            Id = "1",
            PId = "a.1.pId",
            Name = "a.1",
            CompositeId = new CompositeID
            {
                One = "a.1.compositeId.one",
                Two = "a.1.compositeId.two",
                Three = "a.1.compositeId.three"
            }
        },
        new A
        {
            Id = "2",
            PId = "a.2.pId",
            Name = "a.2",
            CompositeId = new CompositeID
            {
                One = "a.2.compositeId.one",
                Two = "a.2.compositeId.two",
                Three = "a.2.compositeId.three"
            }
        }
    ];

    public static readonly IReadOnlyDictionary<string, A> ById =
        Items.ToDictionary(static a => a.Id, StringComparer.Ordinal);
}

namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyDictionary<string, A> AById =
        new Dictionary<string, A>(StringComparer.Ordinal)
        {
            ["1"] = new A
            {
                Id = "1",
                PId = "a.1.pId",
                CompositeId = new CompositeID
                {
                    One = "a.1.compositeId.one",
                    Two = "a.1.compositeId.two",
                    Three = "a.1.compositeId.three"
                }
            },
            ["2"] = new A
            {
                Id = "2",
                PId = "a.2.pId",
                CompositeId = new CompositeID
                {
                    One = "a.2.compositeId.one",
                    Two = "a.2.compositeId.two",
                    Three = "a.2.compositeId.three"
                }
            }
        };

    public static readonly IReadOnlyDictionary<string, B> ById =
        new Dictionary<string, B>(StringComparer.Ordinal)
        {
            ["100"] = new B { Id = "100", A = [AById["1"]] },
            ["200"] = new B { Id = "200", A = [AById["1"], AById["2"]] }
        };
}

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Seed data for the <c>subgraph-a</c> subgraph, containing books and
/// animal references for the <c>provides-on-interface</c> suite.
/// </summary>
internal static class SubgraphAData
{
    /// <summary>
    /// The raw book record: id to animal-id list.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, List<string>> BookAnimalIds =
        new Dictionary<string, List<string>>(StringComparer.Ordinal)
        {
            ["m1"] = ["a1", "a2"]
        };

    /// <summary>
    /// Animal type names indexed by id (used to create the correct concrete type).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AnimalTypes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["a1"] = "Dog",
            ["a2"] = "Cat"
        };
}

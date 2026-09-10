namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Seed data for the <c>subgraph-b</c> subgraph, containing media
/// with animals including their names.
/// </summary>
internal static class SubgraphBData
{
    /// <summary>
    /// The media record with all animal names populated (used by the
    /// <c>@provides(fields: "animals { id name }")</c> path).
    /// </summary>
    public static readonly Book Media = new()
    {
        Id = "m1",
        Animals =
        [
            new Dog { Id = "a1", Name = "Fido" },
            new Cat { Id = "a2", Name = "Whiskers" }
        ]
    };
}

namespace HotChocolate.Fusion.Suites.SharedRoot.Name;

/// <summary>
/// Seed data for the <c>name</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/shared-root/data.ts</c>.
/// </summary>
internal static class NameData
{
    public static readonly Product Product = new()
    {
        Id = "1",
        Name = new Name { Id = "1", Brand = "Brand 1", Model = "Model 1" }
    };
}

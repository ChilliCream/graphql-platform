namespace HotChocolate.Fusion.Suites.SharedRoot.Price;

/// <summary>
/// Seed data for the <c>price</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/shared-root/data.ts</c>.
/// </summary>
internal static class PriceData
{
    public static readonly Product Product = new()
    {
        Id = "1",
        Price = new Price { Id = "1", Amount = 1000, Currency = "USD" }
    };
}

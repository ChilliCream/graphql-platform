using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Accounts;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Inventory;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Products;
using HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("simple-requires-provides")]
public sealed class SimpleRequiresProvidesTests
    : OfficialV2ComplianceTestBase<SimpleRequiresProvidesTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (AccountsSubgraph.Name, AccountsSubgraph.BuildAsync),
            (InventorySubgraph.Name, InventorySubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync),
            (ReviewsSubgraph.Name, ReviewsSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

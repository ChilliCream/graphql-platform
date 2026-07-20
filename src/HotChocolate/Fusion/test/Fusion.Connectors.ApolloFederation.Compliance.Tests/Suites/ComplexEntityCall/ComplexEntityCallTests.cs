using HotChocolate.Fusion.Suites.ComplexEntityCall.Link;
using HotChocolate.Fusion.Suites.ComplexEntityCall.List;
using HotChocolate.Fusion.Suites.ComplexEntityCall.Price;
using HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("complex-entity-call")]
public sealed class ComplexEntityCallTests
    : OfficialV2ComplianceTestBase<ComplexEntityCallTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (LinkSubgraph.Name, LinkSubgraph.BuildAsync),
            (ListSubgraph.Name, ListSubgraph.BuildAsync),
            (PriceSubgraph.Name, PriceSubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

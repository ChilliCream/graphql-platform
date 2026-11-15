using HotChocolate.Fusion.Suites.UnionOverfetching.Card;
using HotChocolate.Fusion.Suites.UnionOverfetching.Product;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("union-overfetching")]
public sealed class UnionOverfetchingTests
    : OfficialV2ComplianceTestBase<UnionOverfetchingTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (CardSubgraph.Name, CardSubgraph.BuildAsync),
            (ProductSubgraph.Name, ProductSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

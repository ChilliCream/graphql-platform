using HotChocolate.Fusion.Suites.MysteriousExternal.Price;
using HotChocolate.Fusion.Suites.MysteriousExternal.Product;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("mysterious-external")]
public sealed class MysteriousExternalTests
    : OfficialV2ComplianceTestBase<MysteriousExternalTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (PriceSubgraph.Name, PriceSubgraph.BuildAsync),
            (ProductSubgraph.Name, ProductSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

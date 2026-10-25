using HotChocolate.Fusion.Suites.SharedRoot.Category;
using HotChocolate.Fusion.Suites.SharedRoot.Name;
using HotChocolate.Fusion.Suites.SharedRoot.Price;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("shared-root")]
public sealed class SharedRootTests
    : OfficialV2ComplianceTestBase<SharedRootTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (NameSubgraph.Name, NameSubgraph.BuildAsync),
            (PriceSubgraph.Name, PriceSubgraph.BuildAsync),
            (CategorySubgraph.Name, CategorySubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

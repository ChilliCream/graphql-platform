using HotChocolate.Fusion.Suites.ParentEntityCallComplex.A;
using HotChocolate.Fusion.Suites.ParentEntityCallComplex.B;
using HotChocolate.Fusion.Suites.ParentEntityCallComplex.C;
using HotChocolate.Fusion.Suites.ParentEntityCallComplex.D;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("parent-entity-call-complex")]
public sealed class ParentEntityCallComplexTests
    : OfficialV2ComplianceTestBase<ParentEntityCallComplexTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync),
            (DSubgraph.Name, DSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

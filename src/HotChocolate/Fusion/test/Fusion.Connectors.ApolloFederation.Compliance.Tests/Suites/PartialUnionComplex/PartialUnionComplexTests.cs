using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Suites.PartialUnionComplex.A;
using HotChocolate.Fusion.Suites.PartialUnionComplex.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("partial-union-complex", ShareableFieldRuntimeTypeRouting = ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)]
public sealed class PartialUnionComplexTests
    : OfficialV2ComplianceTestBase<PartialUnionComplexTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

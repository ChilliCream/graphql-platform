using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;
using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("non-resolvable-interface-object", AllowNonResolvableInterfaceObjects = true)]
public sealed class NonResolvableInterfaceObjectTests
    : OfficialV2ComplianceTestBase<NonResolvableInterfaceObjectTests>
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

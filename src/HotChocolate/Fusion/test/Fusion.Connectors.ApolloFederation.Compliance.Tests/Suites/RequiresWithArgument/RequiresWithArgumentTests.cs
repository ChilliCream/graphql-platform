using HotChocolate.Fusion.Suites.RequiresWithArgument.A;
using HotChocolate.Fusion.Suites.RequiresWithArgument.B;
using HotChocolate.Fusion.Suites.RequiresWithArgument.C;
using HotChocolate.Fusion.Suites.RequiresWithArgument.D;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("requires-with-argument")]
public sealed class RequiresWithArgumentTests
    : OfficialV2ComplianceTestBase<RequiresWithArgumentTests>
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

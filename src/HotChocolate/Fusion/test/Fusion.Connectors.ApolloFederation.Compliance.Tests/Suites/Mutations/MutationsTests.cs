using HotChocolate.Fusion.Suites.Mutations.A;
using HotChocolate.Fusion.Suites.Mutations.B;
using HotChocolate.Fusion.Suites.Mutations.C;
using HotChocolate.Fusion.Suites.Mutations.Shared;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("mutations")]
public sealed class MutationsTests : OfficialV2ComplianceTestBase<MutationsTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
    {
        var state = new MutationsState();

        return ComposeOfficialV2Async(
            (ASubgraph.Name, () => ASubgraph.BuildAsync(state)),
            (BSubgraph.Name, () => BSubgraph.BuildAsync(state)),
            (CSubgraph.Name, () => CSubgraph.BuildAsync(state)));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

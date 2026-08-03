using HotChocolate.Fusion.Suites.SimpleInaccessible.Age;
using HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("simple-inaccessible")]
public sealed class SimpleInaccessibleTests
    : OfficialV2ComplianceTestBase<SimpleInaccessibleTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (FriendsSubgraph.Name, FriendsSubgraph.BuildAsync),
            (AgeSubgraph.Name, AgeSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}

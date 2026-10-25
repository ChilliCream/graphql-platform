using HotChocolate.Fusion.Suites.RequiresCircular.A;
using HotChocolate.Fusion.Suites.RequiresCircular.B;
using Xunit.Sdk;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("requires-circular")]
public sealed class RequiresCircularTests
    : OfficialV2ComplianceTestBase<RequiresCircularTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public async Task OfficialCase_Should_FailComposition_When_RequiringSubgraphServesRequiredParent(
        string caseId)
    {
        // act
        // b serves Post.author and requires author { yearsOfExperience }, which composition rejects
        var exception = await Assert.ThrowsAsync<XunitException>(() => RunOfficialV2CaseAsync(caseId));

        // assert
        exception.Message.ReplaceLineEndings("\n").MatchInlineSnapshot(
            """
            Post-merge validation failed.
            The @require directive on argument 'Post.byNovice(author:)' in schema 'b' specifies an invalid field selection against the composed schema.
             - errors: The required field 'Post.author' must not be served by the requiring source schema 'b'.
            """);
    }
}

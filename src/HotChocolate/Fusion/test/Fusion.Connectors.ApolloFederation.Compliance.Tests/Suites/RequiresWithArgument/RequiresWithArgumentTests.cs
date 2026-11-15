using HotChocolate.Fusion.Suites.RequiresWithArgument.A;
using HotChocolate.Fusion.Suites.RequiresWithArgument.B;
using HotChocolate.Fusion.Suites.RequiresWithArgument.C;
using HotChocolate.Fusion.Suites.RequiresWithArgument.D;
using Xunit.Sdk;

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
    public async Task OfficialCase_Should_FailComposition_When_RequiringSubgraphServesRequiredParent(
        string caseId)
    {
        // act
        // d serves Post.comments and requires comments(limit: 3) { authorId }, which composition rejects
        var exception = await Assert.ThrowsAsync<XunitException>(() => RunOfficialV2CaseAsync(caseId));

        // assert
        exception.Message.ReplaceLineEndings("\n").MatchInlineSnapshot(
            """
            Post-merge validation failed.
            The @require directive on argument 'Post.author(comments:)' in schema 'd' specifies an invalid field selection against the composed schema.
             - errors: The required field 'Post.comments' must not be served by the requiring source schema 'd'.
            """);
    }
}

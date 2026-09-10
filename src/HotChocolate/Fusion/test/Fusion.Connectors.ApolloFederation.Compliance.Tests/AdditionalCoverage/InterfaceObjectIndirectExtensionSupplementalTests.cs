using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;
using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class InterfaceObjectIndirectExtensionSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task EntityRequests_Should_Resolve_When_UpstreamUsesApolloFallbacks()
    {
        await using var a = await ASubgraph.BuildAsync();
        await using var b = await BSubgraph.BuildAsync();
        using var aClient = a.CreateClient();
        using var bClient = b.CreateClient();
        using var aResponse = await aClient.PostAsync(
            "/graphql",
            new StringContent(
                """
                {"query":"query($representations: [_Any!]!) { _entities(representations: $representations) { ... on Media { __typename ... on Video { duration } } } }","variables":{"representations":[{"__typename":"Media","id":"1"}]}}
                """,
                System.Text.Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);
        using var bResponse = await bClient.PostAsync(
            "/graphql",
            new StringContent(
                """
                {"query":"query($representations: [_Any!]!) { _entities(representations: $representations) { ... on Video { authorName } } }","variables":{"representations":[{"__typename":"Video","id":"1"}]}}
                """,
                System.Text.Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);

        ("a:\n"
            + await aResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
            + "\n\nb:\n"
            + await bResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .MatchInlineSnapshot(
                """
                a:
                {"data":{"_entities":[{"__typename":"Video","duration":100}]}}

                b:
                {"data":{"_entities":[{"authorName":"John Doe"}]}}
                """);
    }
}

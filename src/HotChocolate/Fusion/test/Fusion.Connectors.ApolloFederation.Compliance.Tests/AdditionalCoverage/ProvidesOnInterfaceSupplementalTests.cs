using HotChocolate.Execution;
using HotChocolate.Fusion.Suites;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class ProvidesOnInterfaceSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task ProvidedInterfacePath_Should_RouteUnprovidedFieldToOwningSubgraph()
    {
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder
            .ComposeOfficialV2Async<ProvidesOnInterfaceTests>(
                capture,
                (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
                (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync),
                (SubgraphCSubgraph.Name, SubgraphCSubgraph.BuildAsync));
        var testCase = AuditFixture.GetOfficialV2Case<ProvidesOnInterfaceTests>(
            "provides-on-interface/001");

        await gateway.Executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(testCase.Query).Build(),
            TestContext.Current.CancellationToken);

        string.Join(
            "\n\n",
            capture.Requests.Select(
                static request => $"{request.SubgraphName}:\n{request.Body}"))
            .MatchInlineSnapshot(
                """
                b:
                {"query":"query Op_3e0174e7_1 {\n  media {\n    __typename\n    id\n    ... on Book {\n      animals {\n        __typename\n        ... on Cat {\n          id\n        }\n        ... on Dog {\n          id\n        }\n        ... on Cat {\n          name\n        }\n        ... on Dog {\n          name\n        }\n      }\n    }\n  }\n}"}

                c:
                {"query":"query($representations: [_Any!]!) {\n  _entities(representations: $representations) {\n    ... on Cat {\n      age\n    }\n  }\n}","variables":{"representations":[{"__typename":"Cat","id":"a2"}]}}
                """);
    }
}

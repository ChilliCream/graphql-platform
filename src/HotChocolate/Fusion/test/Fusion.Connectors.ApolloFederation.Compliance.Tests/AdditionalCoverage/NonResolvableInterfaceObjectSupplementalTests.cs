using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;
using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class NonResolvableInterfaceObjectSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task Gateway_Should_NotCallSource_When_InterfaceObjectIsNotResolvable()
    {
        var testCase = AuditFixture.GetOfficialV2Case("non-resolvable-interface-object/001");
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            sourceSchemaSettings: null,
            NodeResolution.Gateway,
            allowNonResolvableInterfaceObjects: true,
            ShareableFieldRuntimeTypeRouting.SourceLocal,
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

        var result = await gateway.Executor.ExecuteAsync(
            testCase.Query,
            TestContext.Current.CancellationToken);
        var json = result.ToJson(withIndentations: false);

        AuditAssertions.Assert(json, expectedDataJson: "null", expectsErrors: true);
        Assert.Empty(capture.Requests);
    }
}

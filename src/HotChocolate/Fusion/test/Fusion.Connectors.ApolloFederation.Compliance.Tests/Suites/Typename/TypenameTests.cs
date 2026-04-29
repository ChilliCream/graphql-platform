namespace HotChocolate.Fusion.Suites;

public sealed class TypenameTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Audit subgraph 'b' uses @interfaceObject which is not yet supported by the Apollo Federation adapter. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Pending() => Task.CompletedTask;
}

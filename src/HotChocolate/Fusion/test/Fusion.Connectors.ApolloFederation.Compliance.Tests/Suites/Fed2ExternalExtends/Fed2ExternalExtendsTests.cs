namespace HotChocolate.Fusion.Suites;

public sealed class Fed2ExternalExtendsTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Pending: subgraph harness.")]
    public Task Pending() => Task.CompletedTask;
}

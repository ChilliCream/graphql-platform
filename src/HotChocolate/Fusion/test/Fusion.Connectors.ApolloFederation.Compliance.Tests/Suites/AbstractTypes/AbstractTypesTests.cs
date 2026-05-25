namespace HotChocolate.Fusion.Suites;

public sealed class AbstractTypesTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Pending: abstract-type / edge-case coverage.")]
    public Task Pending() => Task.CompletedTask;
}

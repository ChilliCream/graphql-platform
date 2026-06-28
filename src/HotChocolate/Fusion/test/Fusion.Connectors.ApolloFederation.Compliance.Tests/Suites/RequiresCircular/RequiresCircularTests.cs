namespace HotChocolate.Fusion.Suites;

public sealed class RequiresCircularTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Pending: @requires/@provides coverage.")]
    public Task Pending() => Task.CompletedTask;
}

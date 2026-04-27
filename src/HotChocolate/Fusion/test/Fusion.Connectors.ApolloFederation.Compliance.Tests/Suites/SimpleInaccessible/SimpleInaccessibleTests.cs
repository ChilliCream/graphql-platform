namespace HotChocolate.Fusion.Suites;

public sealed class SimpleInaccessibleTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Pending: @inaccessible currently silently dropped.")]
    public Task Pending() => Task.CompletedTask;
}

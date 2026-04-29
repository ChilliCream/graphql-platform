namespace HotChocolate.Fusion.Suites;

public sealed class Fed1ExternalExtensionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Unsupported: Federation v1 (no @link).")]
    public Task Pending() => Task.CompletedTask;
}

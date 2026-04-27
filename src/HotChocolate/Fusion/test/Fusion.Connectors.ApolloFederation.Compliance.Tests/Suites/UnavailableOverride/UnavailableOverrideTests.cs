namespace HotChocolate.Fusion.Suites;

public sealed class UnavailableOverrideTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Unsupported: @override not yet handled by composition.")]
    public Task Pending() => Task.CompletedTask;
}

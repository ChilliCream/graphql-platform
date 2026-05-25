namespace HotChocolate.Fusion.Suites;

public sealed class SimpleInterfaceObjectTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Unsupported: @interfaceObject rejected by composition.")]
    public Task Pending() => Task.CompletedTask;
}

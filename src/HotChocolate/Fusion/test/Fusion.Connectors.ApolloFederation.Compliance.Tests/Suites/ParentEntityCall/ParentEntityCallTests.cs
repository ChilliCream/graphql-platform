namespace HotChocolate.Fusion.Suites;

public sealed class ParentEntityCallTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => throw new NotImplementedException("Subgraphs not yet wired for this suite.");

    [Fact(Skip = "Composer satisfiability validator cycles on Category.id between subgraphs that both declare Category @key(\"id\") without a non-lookup path. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Pending() => Task.CompletedTask;
}

namespace HotChocolate.Fusion;

public abstract class OfficialV1ComplianceTestBase<TSuite> : ComplianceTestBase
    where TSuite : OfficialV1ComplianceTestBase<TSuite>
{
    public static TheoryData<string> Cases => AuditFixture.GetOfficialV1CaseIds<TSuite>();

    protected Task RunOfficialV1CaseAsync(string caseId)
        => OfficialAuditSuiteRun<TSuite>.AssertCaseAsync(
            caseId,
            BuildGatewayAsync,
            static () => AuditFixture.GetOfficialV1Suite<TSuite>());

    protected Task<FusionGateway> ComposeOfficialV1Async(
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => FusionGatewayBuilder.ComposeOfficialV1Async<TSuite>(subgraphs);

    protected Task<FusionGateway> ComposeOfficialV1Async(
        SubgraphRequestCapture capture,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => FusionGatewayBuilder.ComposeOfficialV1Async<TSuite>(capture, subgraphs);
}

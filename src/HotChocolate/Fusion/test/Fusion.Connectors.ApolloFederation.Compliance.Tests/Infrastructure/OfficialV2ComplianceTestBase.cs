using System.Runtime.ExceptionServices;

namespace HotChocolate.Fusion;

public abstract class OfficialV2ComplianceTestBase<TSuite> : ComplianceTestBase
    where TSuite : OfficialV2ComplianceTestBase<TSuite>
{
    public static TheoryData<string> Cases => AuditFixture.GetOfficialV2CaseIds<TSuite>();

    protected Task RunOfficialV2CaseAsync(string caseId)
        => OfficialAuditSuiteRun<TSuite>.AssertCaseAsync(
            caseId,
            BuildGatewayAsync,
            static () => AuditFixture.GetOfficialV2Suite<TSuite>());

    protected Task<FusionGateway> ComposeOfficialV2Async(
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => FusionGatewayBuilder.ComposeOfficialV2Async<TSuite>(subgraphs);

    protected Task<FusionGateway> ComposeOfficialV2Async(
        SubgraphRequestCapture capture,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => FusionGatewayBuilder.ComposeOfficialV2Async<TSuite>(capture, subgraphs);
}

internal static class OfficialAuditSuiteRun<TSuite>
{
    private static readonly TimeSpan s_caseTimeout = TimeSpan.FromSeconds(10);
    private static readonly object s_sync = new();
    private static Lazy<Task<IReadOnlyDictionary<string, ExceptionDispatchInfo?>>>? s_run;

    public static async Task AssertCaseAsync(
        string caseId,
        Func<Task<FusionGateway>> buildGatewayAsync,
        Func<OfficialAuditSuite> getSuite)
    {
        var run = Volatile.Read(ref s_run);

        if (run is null)
        {
            lock (s_sync)
            {
                var cancellationToken = TestContext.Current.CancellationToken;
                run = s_run ??= new Lazy<Task<IReadOnlyDictionary<string, ExceptionDispatchInfo?>>>(
                    () => ExecuteSuiteAsync(buildGatewayAsync, getSuite(), cancellationToken),
                    LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }

        var outcomes = await run.Value.ConfigureAwait(false);

        if (outcomes[caseId] is { } error)
        {
            error.Throw();
        }
    }

    private static async Task<IReadOnlyDictionary<string, ExceptionDispatchInfo?>> ExecuteSuiteAsync(
        Func<Task<FusionGateway>> buildGatewayAsync,
        OfficialAuditSuite suite,
        CancellationToken cancellationToken)
    {
        var outcomes = new Dictionary<string, ExceptionDispatchInfo?>(StringComparer.Ordinal);
        FusionGateway gateway;

        try
        {
            gateway = await buildGatewayAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var error = ExceptionDispatchInfo.Capture(exception);

            foreach (var testCase in suite.Cases)
            {
                outcomes.Add(testCase.Id, error);
            }

            return outcomes;
        }

        await using (gateway.ConfigureAwait(false))
        {
            foreach (var testCase in suite.Cases)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);
                    timeout.CancelAfter(s_caseTimeout);

                    await ComplianceTestBase.ExecuteAndAssertAsync(
                        gateway,
                        testCase,
                        timeout.Token).ConfigureAwait(false);
                    outcomes.Add(testCase.Id, null);
                }
                catch (Exception exception)
                {
                    outcomes.Add(testCase.Id, ExceptionDispatchInfo.Capture(exception));
                }
            }
        }

        return outcomes;
    }
}

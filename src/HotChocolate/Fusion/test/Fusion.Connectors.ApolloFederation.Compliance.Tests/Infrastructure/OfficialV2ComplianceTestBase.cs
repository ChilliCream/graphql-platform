using System.Runtime.ExceptionServices;

namespace HotChocolate.Fusion;

public abstract class OfficialV2ComplianceTestBase<TSuite> : ComplianceTestBase
    where TSuite : OfficialV2ComplianceTestBase<TSuite>
{
    public static TheoryData<string> Cases => AuditFixture.GetOfficialV2CaseIds<TSuite>();

    protected Task RunOfficialV2CaseAsync(string caseId)
        => OfficialV2SuiteRun<TSuite>.AssertCaseAsync(caseId, BuildGatewayAsync);

    protected Task<FusionGateway> ComposeOfficialV2Async(
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => FusionGatewayBuilder.ComposeOfficialV2Async<TSuite>(subgraphs);
}

internal static class OfficialV2SuiteRun<TSuite>
{
    private static readonly object s_sync = new();
    private static Lazy<Task<IReadOnlyDictionary<string, ExceptionDispatchInfo?>>>? s_run;

    public static async Task AssertCaseAsync(
        string caseId,
        Func<Task<FusionGateway>> buildGatewayAsync)
    {
        var run = Volatile.Read(ref s_run);

        if (run is null)
        {
            lock (s_sync)
            {
                run = s_run ??= new Lazy<Task<IReadOnlyDictionary<string, ExceptionDispatchInfo?>>>(
                    () => ExecuteSuiteAsync(buildGatewayAsync),
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
        Func<Task<FusionGateway>> buildGatewayAsync)
    {
        var suite = AuditFixture.GetOfficialV2Manifest().Suites.Single(
            candidate => candidate.Id == AuditFixture.GetOfficialV2SuiteAttribute<TSuite>().Id);
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
                    await ComplianceTestBase.ExecuteAndAssertAsync(
                        gateway,
                        testCase,
                        CancellationToken.None).ConfigureAwait(false);
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

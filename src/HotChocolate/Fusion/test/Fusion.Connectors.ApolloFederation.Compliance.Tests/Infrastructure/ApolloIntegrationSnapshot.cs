using HotChocolate.Execution;
using HotChocolate.Fusion.Testing;

namespace HotChocolate.Fusion;

internal static class ApolloIntegrationSnapshot
{
    public static Task MatchAsync(
        Snapshot snapshot,
        string document,
        SubgraphRequestCapture capture,
        IExecutionResult result,
        bool includeRequestOrder)
        => FusionTestBase.MatchExchangeSnapshotAsync(
            snapshot,
            document,
            result,
            capture.Requests.Select(r => new FusionTestBase.HttpExchange(
                r.SubgraphName,
                r.Body,
                r.StatusCode,
                r.ContentType,
                r.ResponseBody)).ToArray(),
            includeRequestOrder);
}

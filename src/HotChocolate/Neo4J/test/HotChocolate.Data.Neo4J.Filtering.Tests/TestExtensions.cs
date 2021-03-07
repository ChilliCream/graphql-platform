using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public static class TestExtensions
    {
        public static void MatchDocumentSnapshot(
            this IExecutionResult? result,
            string snapshotName)
        {
            if (result is null) return;
            result.ToJson().MatchSnapshot(new SnapshotNameExtension(snapshotName));
            if (result.ContextData is { } &&
                result.ContextData.TryGetValue("query", out object? queryResult) &&
                queryResult is string queryString &&
                !string.IsNullOrWhiteSpace(queryString))
            {
                queryString.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_query"));
            }
        }
    }
}

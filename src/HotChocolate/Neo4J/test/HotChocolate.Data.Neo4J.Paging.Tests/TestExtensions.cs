using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.Neo4J.Paging
{
    public static class TestExtensions
    {
        public static void MatchDocumentSnapshot(
            this IExecutionResult result,
            string snapshotName = "")
        {
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

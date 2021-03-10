using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public static class TestExtensions
    {
        public static void MatchDocumentSnapshot(
            this IExecutionResult? result,
            string snapshotName)
        {
            if (result is null) return;

            if (result.ContextData is { } &&
                result.ContextData.TryGetValue("query", out object? queryResult))
            {
                queryResult.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_query"));
            }

            result.ToJson().MatchSnapshot(new SnapshotNameExtension(snapshotName));
        }
    }
}

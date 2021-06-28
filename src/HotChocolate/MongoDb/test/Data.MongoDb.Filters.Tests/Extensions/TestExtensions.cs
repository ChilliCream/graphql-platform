using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.MongoDb.Filters
{
    public static class TestExtensions
    {
        public static void MatchDocumentSnapshot(
            this IExecutionResult? result,
            string snapshotName)
        {
            if (result is { })
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
}

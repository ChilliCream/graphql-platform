using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.MongoDb.Projections
{
    public static class TestExtensions
    {
        public static void MatchDocumentSnapshot(
            this IExecutionResult? result,
            string snapshotName = "")
        {
            if (result is { })
            {
                result.ToJson().MatchSnapshot(snapshotName);
                if (result.ContextData is { } &&
                    result.ContextData.TryGetValue("query", out object? queryResult))
                {
                    queryResult.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_query"));
                }
            }
        }
    }
}

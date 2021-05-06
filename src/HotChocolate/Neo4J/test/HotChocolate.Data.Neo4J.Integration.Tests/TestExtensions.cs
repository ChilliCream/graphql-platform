using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.Neo4J.Integration
{
    public static class TestExtensions
    {
        public static void MatchDocumentSnapshot(
            this IExecutionResult? result,
            string snapshotName)
        {
            if (result is null) return;

            result.ToJson().MatchSnapshot(new SnapshotNameExtension(snapshotName));
        }
    }
}

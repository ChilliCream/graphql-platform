using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.SqlKata.Filters
{
    public static class TestExtensions
    {
        public static void MatchSqlSnapshot(
            this IExecutionResult? result,
            string snapshotName)
        {
            if (result is { })
            {
                if (result.ContextData is { } &&
                    result.ContextData.TryGetValue("sql", out object? queryResult))
                {
                    queryResult.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_sql"));
                }
                result.ToJson().MatchSnapshot(new SnapshotNameExtension(snapshotName));
            }
        }
    }
}

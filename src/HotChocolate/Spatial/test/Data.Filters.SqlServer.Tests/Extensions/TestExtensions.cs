using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.Filters.Spatial
{
    public static class TestExtensions
    {
        public static void MatchSqlSnapshot(
            this IExecutionResult? result,
            string snapshotName)
        {
            if (result is { })
            {
                result.MatchSnapshot(snapshotName);
                if (result.ContextData is { } &&
                    result.ContextData.TryGetValue("sql", out object? queryResult))
                {
                    queryResult.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_sql"));
                }
            }
        }
    }
}

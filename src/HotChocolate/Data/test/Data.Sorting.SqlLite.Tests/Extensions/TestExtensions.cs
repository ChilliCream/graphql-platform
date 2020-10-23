using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.Sorting
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
                    result.ContextData.TryGetValue("sql", out var queryResult))
                {
                    queryResult.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_sql"));
                }

                result.MatchSnapshot(snapshotName);
            }
        }
    }
}

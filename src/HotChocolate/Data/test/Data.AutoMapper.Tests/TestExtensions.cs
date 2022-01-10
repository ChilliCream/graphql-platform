using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.Projections;

public static class TestExtensions
{
    public static void MatchSqlSnapshot(
        this IExecutionResult? result,
        string snapshotName = "")
    {
#if NET5_0
            const string postfix = "_NET5_0";
#elif NET6_0
            const string postfix = "_NET6_0";
#else
            const string postfix = "";
#endif
        if (result is null)
        {
            return;
        }

        result.ToJson().MatchSnapshot(new SnapshotNameExtension(snapshotName + postfix));

        if (result.ContextData is null)
        {
            return;
        }

        if (result.ContextData.TryGetValue("sql", out var value))
        {
            SnapshotNameExtension extension = new(snapshotName + "sql" + postfix);
            value.MatchSnapshot(extension);
        }

        if (result.ContextData.TryGetValue("expression", out value))
        {
            SnapshotNameExtension extension = new(snapshotName + "expression" + postfix);
            value.MatchSnapshot(extension);
        }
    }
}

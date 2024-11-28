using HotChocolate.Execution;
using static CookieCrumble.Formatters.SnapshotValueFormatters;

namespace HotChocolate.Data.Projections;

public static class TestExtensions
{
    public static void AddSqlFrom(
        this Snapshot snapshot,
        IExecutionResult result)
    {
        snapshot.Add(result.ToJson(), "Result:");

        if (result.ContextData is null)
        {
            return;
        }

        if (result.ContextData.TryGetValue("sql", out var sql))
        {
            snapshot.Add(sql, "SQL:", PlainText);
        }

        if (result.ContextData.TryGetValue("expression", out var expression))
        {
            snapshot.Add(expression, "Expression:", PlainText);
        }
    }
}

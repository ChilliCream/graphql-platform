using CookieCrumble;
using HotChocolate.Execution;
using static CookieCrumble.Formatters.SnapshotValueFormatters;

namespace HotChocolate.Data.Filters;

public static class TestExtensions
{
    public static Snapshot AddSqlFrom(
        this Snapshot snapshot,
        IExecutionResult result,
        string name)
    {
        snapshot.Add(result.ToJson(), $"{name} Result:");

        if (result.ContextData is null)
        {
            return snapshot;
        }

        if (result.ContextData.TryGetValue("sql", out var sql))
        {
            snapshot.Add(sql, $"{name} SQL:", PlainText);
        }

        if (result.ContextData.TryGetValue("expression", out var expression))
        {
            snapshot.Add(expression, $"{name} Expression:", PlainText);
        }

        return snapshot;
    }
}

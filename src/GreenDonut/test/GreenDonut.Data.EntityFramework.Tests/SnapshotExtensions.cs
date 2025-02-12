namespace GreenDonut.Data;

public static class SnapshotExtensions
{
    public static Snapshot AddQueries(
        this Snapshot snapshot,
        List<QueryInfo> queries)
    {
        for (var i = 0; i < queries.Count; i++)
        {
            snapshot
                .Add(queries[i].QueryText, $"SQL {i}", "sql")
                .Add(queries[i].ExpressionText, $"Expression {i}");
        }

        return snapshot;
    }
}

using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Filtering;

internal static class Neo4JFilterScopeExtensions
{
    public static string GetPath(this Neo4JFilterScope scope) =>
        string.Join(".", scope.Path.Reverse());

    public static CompoundCondition CreateQuery(this Neo4JFilterScope scope)
    {
        if (scope.Level.Peek().Count == 0)
        {
            return new CompoundCondition(null);
        }

        var conditions = new CompoundCondition(Operator.And);
        foreach (var condition in scope.Level.Peek().ToArray())
        {
            conditions.And(condition);
        }

        return conditions;
    }
}

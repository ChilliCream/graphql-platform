using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    internal static class Neo4JFilterScopeExtensions
    {
        public static string GetPath(this Neo4JFilterScope scope) =>
            string.Join(".", scope.Path.Reverse());

        public static bool TryCreateQuery(
            this Neo4JFilterScope scope,
            [NotNullWhen(true)] out CompoundCondition query)
        {
            query = null;

            if (scope.Level.Peek().Count == 0)
            {
                return false;
            }

            var conditions = new CompoundCondition(Operator.And);
            foreach (Condition condition in scope.Level.Peek().ToArray())
            {
                conditions.And(condition);
            }

            query = conditions;

            return true;
        }
    }

}

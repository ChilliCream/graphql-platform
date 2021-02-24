using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Filtering
{
    internal static class Neo4JFilterScopeExtensions
    {
        public static string GetPath(this Neo4JFilterScope scope) =>
            string.Join(".", scope.Path.Reverse());

        public static bool TryCreateQuery(
            this Neo4JFilterScope scope,
            [NotNullWhen(true)] out Neo4JFilterDefinition query)
        {
            query = null;

            if (scope.Level.Peek().Count == 0)
            {
                return false;
            }

            // TODO: implement AndFilterDefinition
            // query = new AndFilterDefinition(scope.Level.Peek().ToArray());

            return true;
        }
    }

}

using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public static class Neo4JFilteringVisitorContextExtensions
    {
        /// <summary>
        /// Reads the current scope from the context
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>The current scope</returns>
        public static Neo4JFilterScope GetNeo4JFilterScope(
            this Neo4JFilterVisitorContext context) =>
            (Neo4JFilterScope)context.GetScope();

        /// <summary>
        /// Tries to build the query based on the items that are stored on the scope
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="query">The query that was build</param>
        /// <returns>True in case the query has been build successfully, otherwise false</returns>
        public static bool TryCreateQuery(
            this Neo4JFilterVisitorContext context,
            [NotNullWhen(true)] out Neo4JFilterDefinition query)
        {
            return context.GetNeo4JFilterScope().TryCreateQuery(out query);
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using ServiceStack;

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

        public static Node GetNode(
            this Neo4JFilterVisitorContext context)
        {
            var nodeName = context.RuntimeTypes.Last().Type.Name;

            return Cypher.Node(nodeName).Named(nodeName.ToCamelCase());
        }

        /// <summary>
        /// Tries to build the query based on the items that are stored on the scope
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="query">The query that was build</param>
        /// <returns>True in case the query has been build successfully, otherwise false</returns>
        public static bool TryCreateQuery(
            this Neo4JFilterVisitorContext context,
            [NotNullWhen(true)] out CompoundCondition query)
        {
            return context.GetNeo4JFilterScope().TryCreateQuery(out query);
        }
    }
}

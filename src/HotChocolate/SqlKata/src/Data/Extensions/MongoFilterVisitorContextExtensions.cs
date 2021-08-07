using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    public static class MongoFilterVisitorContextExtensions
    {
        /// <summary>
        /// Reads the current scope from the context
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>The current scope</returns>
        public static SqlKataFilterScope GetSqlKataFilterScope(
            this SqlKataFilterVisitorContext context) =>
            (SqlKataFilterScope)context.GetScope();

        /// <summary>
        /// Tries to build the query based on the items that are stored on the scope
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="query">The query that was build</param>
        /// <returns>True in case the query has been build successfully, otherwise false</returns>
        public static bool TryCreateQuery(
            this SqlKataFilterVisitorContext context,
            [NotNullWhen(true)] out Query? query)
        {
            return context.GetSqlKataFilterScope().TryCreateQuery(out query);
        }
    }
}

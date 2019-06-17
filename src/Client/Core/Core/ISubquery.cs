using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// Represents a query which fetches subsequent pages of data after a master query has run.
    /// </summary>
    public interface ISubquery : ICompiledQuery
    {
        /// <summary>
        /// Gets a method which reads the parent IDs from the master query results.
        /// </summary>
        Func<JObject, IEnumerable<JToken>> ParentIds { get; }

        /// <summary>
        /// Gets a method which reads the query paging information from the subquery results.
        /// </summary>
        Func<JObject, JToken> PageInfo { get; }

        /// <summary>
        /// Gets a method which reads the query paging information the master query results.
        /// </summary>
        Func<JObject, IEnumerable<JToken>> ParentPageInfo { get; }

        /// <summary>
        /// Gets a query runner to run the subquery to completion.
        /// </summary>
        /// <param name="connection">The connection on which to run the query.</param>
        /// <param name="id">The ID of the parent object.</param>
        /// <param name="after">The end cursor from the master query.</param>
        /// <param name="variables">The query variables.</param>
        /// <param name="addResult">The method to call to add an item to the result collection.</param>
        /// <returns>An <see cref="IQueryRunner"/>.</returns>
        IQueryRunner Start(
            IConnection connection,
            string id,
            string after,
            IDictionary<string, object> variables,
            Action<object> addResult);
    }
}

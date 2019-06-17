using System;
using System.Collections;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// Represents an <see cref="IQueryRunner"/> for an <see cref="ISubquery"/>.
    /// </summary>
    public interface ISubqueryRunner : IQueryRunner
    {
        /// <summary>
        /// Called to tell the runner where a specified subquery should store its results.
        /// </summary>
        /// <param name="query">The subquery.</param>
        /// <param name="add">The method to call to add an item to the target collection.</param>
        void SetQueryResultSink(ISubquery query, Action<object> add);
    }
}

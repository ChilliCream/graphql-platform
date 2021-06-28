using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    /// <inheritdoc />
    public interface IFilterVisitorContext<T>
        : IFilterVisitorContext
    {
        /// <summary>
        /// The different scopes of the visitor
        /// </summary>
        Stack<FilterScope<T>> Scopes { get; }

        /// <summary>
        /// Creates a new scope of the visitor
        /// </summary>
        /// <returns>The created scope</returns>
        FilterScope<T> CreateScope();
    }
}

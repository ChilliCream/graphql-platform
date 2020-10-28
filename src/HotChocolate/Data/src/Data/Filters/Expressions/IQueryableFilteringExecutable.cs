using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// If this contract is implemented a <see cref="QueryableFilterProvider"/> can set the filter
    /// expression
    /// </summary>
    public interface IQueryableFilteringExecutable<T>
    {
        /// <summary>
        /// Checks if the underlying source is a <see cref="IEnumerable{T}"/> or a
        /// <see cref="IQueryable{T}"/>
        /// </summary>
        /// <returns></returns>
        bool IsInMemory();

        /// <summary>
        /// Enables or disables filtering on this executable
        /// </summary>
        /// <param name="filter">
        /// Sets the expression that filters the source data. If the expression is null it will skip
        /// filtering
        /// </param>
        IExecutable ApplyFiltering(Expression<Func<T, bool>>? filter);
    }
}

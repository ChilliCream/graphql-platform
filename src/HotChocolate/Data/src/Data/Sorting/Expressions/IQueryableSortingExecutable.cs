using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Sorting.Expressions
{
    /// <summary>
    /// If this contract is implemented a <see cref="QueryableSortProvider"/> can set the filter
    /// expression
    /// </summary>
    public interface IQueryableSortingExecutable<T>
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
        /// <param name="sort">
        /// Sets the delegate that applies sorting to the source data. If the delegate is null it
        /// will skip sorting
        /// </param>
        IExecutable ApplySorting(Func<IQueryable<T>,IQueryable<T>>? sort);
    }
}

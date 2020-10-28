using System;
using System.Linq.Expressions;

namespace HotChocolate.Data.Projections.Expressions
{
    /// <summary>
    /// If this contract is implemented a <see cref="QueryableProjectionProvider"/> can set the filter
    /// expression
    /// </summary>
    public interface IQueryableProjectionExecutable<T>
    {
        /// <summary>
        /// Enables or disables filtering on this executable
        /// </summary>
        /// <param name="projection">
        /// Sets the expression that filters the source data. If the expression is null it will skip
        /// filtering
        /// </param>
        IExecutable ApplyProjection(Expression<Func<T, T>>? projection);
    }
}

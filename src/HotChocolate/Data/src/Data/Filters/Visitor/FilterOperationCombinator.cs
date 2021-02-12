using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The <see cref="FilterVisitor{TContext,T}"/> combines operation every time a
    /// <see cref="ObjectValueNode"/> is left. The visitor uses this combinator to combine the
    /// operation of the value node. The combinator is also used to combine
    /// <see cref="FilterCombinator.Or"/> and <see cref="FilterCombinator.And"/> fields
    /// </summary>
    public abstract class FilterOperationCombinator
    {
        /// <summary>
        /// Tries to combine all operations provided by <paramref name="operations"/> with the kind
        /// of combinator specified bu <paramref name="combinator"/>.
        /// </summary>
        /// <param name="context">The context of the visitor</param>
        /// <param name="operations">The operations to combine</param>
        /// <param name="combinator">The kind of combinator that should be applied</param>
        /// <param name="combined">
        /// The combined operations as a new instance of <typeparamref name="T"/>
        /// </param>
        /// <typeparam name="TContext">The type of the context</typeparam>
        /// <typeparam name="T">The type of the combined object</typeparam>
        /// <returns>True if the combination was successful</returns>
        public abstract bool TryCombineOperations<TContext, T>(
            TContext context,
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined)
            where TContext : FilterVisitorContext<T>;
    }
}

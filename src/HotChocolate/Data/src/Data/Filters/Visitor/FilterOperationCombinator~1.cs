using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.Filters
{
    /// <inheritdoc />
    public abstract class FilterOperationCombinator<TContext, T>
        : FilterOperationCombinator
        where TContext : FilterVisitorContext<T>
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
        /// <returns>True if the combination was successful</returns>
        public abstract bool TryCombineOperations(
            TContext context,
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined);

        /// <inheritdoc />
        public override bool TryCombineOperations<TVisitorContext, TOperation>(
            TVisitorContext context,
            Queue<TOperation> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out TOperation combined)
        {
            if (operations is Queue<T> operationsOfT &&
                context is TContext contextOfT &&
                TryCombineOperations(
                    contextOfT,
                    operationsOfT,
                    combinator,
                    out T combinedOfT) &&
                combinedOfT is TOperation combinedOperation)
            {
                combined = combinedOperation;
                return true;
            }

            combined = default!;
            return false;
        }
    }
}

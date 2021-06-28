using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The base of a operation handler that can be bound to a <see cref="FilterOperationField"/>.
    /// The is executed during the visitation of a input object. This base is optimized to handle
    /// filter operations
    /// </summary>
    public abstract class FilterOperationHandler<TContext, T>
        : FilterFieldHandler<TContext, T>
        where TContext : FilterVisitorContext<T>
    {
        /// <inheritdoc/>
        public override bool TryHandleEnter(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field is IFilterOperationField filterOperationField &&
                TryHandleOperation(context, filterOperationField,  node, out T result))
            {
                context.GetLevel().Enqueue(result);
                action = SyntaxVisitor.SkipAndLeave;
            }
            else
            {
                action = SyntaxVisitor.Break;
            }

            return true;
        }

        /// <summary>
        /// Maps a operation field to a provider specific result.
        /// This method is called when the <see cref="FilterVisitor{TContext,T}"/> enters a
        /// field
        /// </summary>
        /// <param name="context">The <see cref="IFilterVisitorContext{T}"/> of the visitor</param>
        /// <param name="field">The field that is currently being visited</param>
        /// <param name="node">The value node of this field</param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        public virtual bool TryHandleOperation(
            TContext context,
            IFilterOperationField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out T result)
        {
            result = default!;
            return false;
        }
    }
}

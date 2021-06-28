using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The base of a handler that can be bound to a <see cref="FilterField"/>. The handler is
    /// executed during the visitation of a input object.
    /// </summary>
    public abstract class FilterFieldHandler<TContext, T>
        : IFilterFieldHandler<TContext, T>
        where TContext : FilterVisitorContext<T>
    {
        /// <inheritdoc />
        public virtual bool TryHandleEnter(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        /// <inheritdoc />
        public virtual bool TryHandleLeave(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        /// <inheritdoc />
        public abstract bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition);
    }
}

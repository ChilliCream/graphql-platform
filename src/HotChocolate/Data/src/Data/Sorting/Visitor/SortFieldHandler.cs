using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting
{
    public abstract class SortFieldHandler<TContext, T>
        : ISortFieldHandler<TContext, T>
        where TContext : SortVisitorContext<T>
    {
        /// <inheritdoc />
        public virtual bool TryHandleEnter(
            TContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        /// <inheritdoc />
        public virtual bool TryHandleLeave(
            TContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        /// <inheritdoc />
        public abstract bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeDefinition typeDefinition,
            ISortFieldDefinition fieldDefinition);
    }
}

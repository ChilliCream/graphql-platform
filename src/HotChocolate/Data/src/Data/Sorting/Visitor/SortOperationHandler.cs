using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public abstract class SortOperationHandler<TContext, T>
        : ISortOperationHandler<TContext, T>
        where TContext : SortVisitorContext<T>
    {
        public virtual bool TryHandleEnter(
            TContext context,
            ISortField field,
            ISortEnumValue? sortValue,
            EnumValueNode node,
            [NotNullWhen(true)]out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        /// <inheritdoc />
        public abstract bool CanHandle(
            ITypeCompletionContext context,
            EnumTypeDefinition typeDefinition,
            SortEnumValueDefinition valueDefinition);
    }
}

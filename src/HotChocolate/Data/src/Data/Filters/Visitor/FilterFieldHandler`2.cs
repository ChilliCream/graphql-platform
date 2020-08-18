using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldHandler<T, TContext>
        : FilterFieldHandler
        where TContext : FilterVisitorContext<T>
    {
        public virtual bool TryHandleEnter(
            TContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        public virtual bool TryHandleLeave(
            TContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) => false;
    }
}

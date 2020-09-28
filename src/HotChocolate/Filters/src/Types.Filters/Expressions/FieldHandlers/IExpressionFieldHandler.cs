using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters.Expressions
{
    public interface IExpressionFieldHandler
    {
        bool Enter(FilterOperationField field,
            ObjectFieldNode node,
            IQueryableFilterVisitorContext context,
            out ISyntaxVisitorAction action);

        void Leave(FilterOperationField field,
            ObjectFieldNode node,
            IQueryableFilterVisitorContext context);
    }
}


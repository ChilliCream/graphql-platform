using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters.Expressions
{
    [Obsolete("Use HotChocolate.Data.")]
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


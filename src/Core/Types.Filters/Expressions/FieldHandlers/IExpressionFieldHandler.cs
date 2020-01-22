using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public interface IExpressionFieldHandler
    {
        bool Enter(
            IQueryableFilterVisitorContext context,
            FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            out VisitorAction action);

        void Leave(
            IQueryableFilterVisitorContext context,
            FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors);
    }
}


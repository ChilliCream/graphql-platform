using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public interface IExpressionFieldHandler
    {
        bool Enter(FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            Stack<QueryableClosure> closures,
            out VisitorAction action
        );

        void Leave(FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            Stack<QueryableClosure> closures
        );
    }
}


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
            Stack<Queue<Expression>> level,
            Stack<Expression> instance,
            out VisitorAction action
        );

        void Leave(FilterOperationField field,
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors,
            Stack<Queue<Expression>> level,
            Stack<Expression> instance
        );
    }
}


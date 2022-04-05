using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents a list of expressions. When visited, the expressions are treated as named
    /// expression if they have declared a symbolic name as variable or as unnamed expression
    /// when nameless. Not to be mixed up with the actual ListExpression, which itself is an
    /// expression.
    /// </summary>
    public class ExpressionList
        : TypedSubtree<Expression>
        , ITypedSubtree
    {
        public ExpressionList(List<Expression> returnItems) : base(returnItems)
        {
        }

        public ExpressionList(params Expression[] returnItems) : base(returnItems)
        {
        }

        public override ClauseKind Kind => ClauseKind.ExpressionList;

        protected override IVisitable PrepareVisit(Expression child) =>
            Expressions.NameOrExpression(child);
    }
}

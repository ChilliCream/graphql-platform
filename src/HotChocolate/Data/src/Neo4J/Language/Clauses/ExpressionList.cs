using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// <para>
    /// Represents a list of expressions. When visited, the expressions are treated as named expression if they have declared
    /// a symbolic name as variable or as unnamed expression when nameless.
    /// </para>
    /// <para>Not to be mixed up with the actual ListExpression, which itself is an expression.</para>
    /// </summary>
    public class ExpressionList : TypedSubtree<Expression, ExpressionList>
    {
        public override ClauseKind Kind => ClauseKind.Default;
        public ExpressionList(List<Expression> returnItems) : base(returnItems) { }
        public ExpressionList(Expression[] returnItems) : base(returnItems) { }

        public new IVisitable PrepareVisit(Expression child) => Expressions.NameOrExpression(child);
    }
}

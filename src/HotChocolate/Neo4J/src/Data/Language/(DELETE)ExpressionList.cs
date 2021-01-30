using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// <para>
    /// Represents a list of expressions. When visited, the expressions are treated as named expression if they have declared
    /// a symbolic name as variable or as unnamed expression when nameless.
    /// </para>
    /// <para>Not to be mixed up with the actual ListExpression, which itself is an expression.</para>
    /// </summary>
    public class ExpressionList : Visitable
    {
        public override ClauseKind Kind => ClauseKind.ExpressionList;
        private readonly List<Expression> _expressions;

        public ExpressionList(List<Expression> returnItems)
        {
            _expressions = returnItems;
        }

        public ExpressionList(params Expression[] returnItems)
        {
            _expressions = returnItems.ToList();
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _expressions.ForEach(e => PrepareVisit(e).Visit(visitor));
            visitor.Leave(this);
        }

        protected static Visitable PrepareVisit(Expression child) {
            return Expressions.NameOrExpression(child);
        }
    }
}

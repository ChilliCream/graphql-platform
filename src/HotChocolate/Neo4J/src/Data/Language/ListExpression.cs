namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents a list expression as in [expression1, expression2, ..., expressionN]
    /// </summary>
    public class ListExpression : Expression
    {
        public override ClauseKind Kind => ClauseKind.ListExpression;
        private readonly ExpressionList _content;

        public ListExpression(ExpressionList content)
        {
            _content = content;
        }

        public static Expression ListOrSingleExpression(params Expression[] expressions) {

            Ensure.IsNotNull(expressions, "Expressions are required.");
            Ensure.IsNotEmpty(expressions, "At least one expression is required.");

            return expressions.Length == 1 ? expressions[0] : Create(expressions);
        }

        public static ListExpression Create(params Expression[] expressions) {

            return new (new ExpressionList(expressions));
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _content.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}

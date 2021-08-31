namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents a list expression as in [expression1, expression2, ..., expressionN]
    /// </summary>
    public class ListExpression : Expression
    {
        public ListExpression(ExpressionList content)
        {
            Content = content;
        }

        public override ClauseKind Kind => ClauseKind.ListExpression;

        public ExpressionList Content { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Content.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }

        public static Expression ListOrSingleExpression(params Expression[] expressions)
        {
            Ensure.IsNotNull(expressions, "Expressions are required.");
            Ensure.IsNotEmpty(expressions, "At least one expression is required.");

            return expressions.Length == 1 ? expressions[0] : Create(expressions);
        }

        public static ListExpression Create(params Expression[] expressions)
        {
            return new(new ExpressionList(expressions));
        }
    }
}

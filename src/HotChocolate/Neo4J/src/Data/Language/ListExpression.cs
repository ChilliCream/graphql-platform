namespace HotChocolate.Data.Neo4J.Language
{
    public class ListExpression : Expression
    {
        public override ClauseKind Kind { get; } = ClauseKind.ListExpression;

        public static Expression ListOrSingleExpression(params Expression[] expressions) {

            Ensure.IsNotNull(expressions, "Expressions are required.");
            Ensure.IsNotNull(expressions, "At least one expression is required.");

            return expressions.Length == 1 ? expressions[0] : Create(expressions);
        }

        private static ListExpression Create(params Expression[] expressions) {

            return new (new ExpressionList(expressions));
        }

        private readonly ExpressionList _content;

        public ListExpression(ExpressionList content) {
            _content = content;
        }

        public override void Visit(CypherVisitor cypherVisitor) {

            cypherVisitor.Enter(this);
            _content.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}

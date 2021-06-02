namespace HotChocolate.Data.Neo4J.Language
{
    public class SortItem : Visitable
    {
        public SortItem(Expression expression, SortDirection? direction)
        {
            Expression = expression;
            Direction = direction;
        }

        public override ClauseKind Kind => ClauseKind.SortItem;

        public Expression Expression { get; }

        public SortDirection? Direction { get; }

        public SortItem Ascending() => new(Expression, SortDirection.Ascending);

        public SortItem Descending() => new(Expression, SortDirection.Descending);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Expressions.NameOrExpression(Expression).Visit(cypherVisitor);

            if (Direction != SortDirection.Undefined)
            {
                Direction?.Visit(cypherVisitor);
            }

            cypherVisitor.Leave(this);
        }

        public static SortItem Create(Expression expression, SortDirection? direction)
        {
            return new(expression, direction ?? SortDirection.Undefined);
        }
    }
}

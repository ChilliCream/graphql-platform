#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    public class SortItem : Visitable
    {
        public override ClauseKind Kind => ClauseKind.SortItem;
        private readonly Expression _expression;
        private readonly SortDirection? _direction;


        private SortItem(Expression expression, SortDirection? direction)
        {
            _expression = expression;
            _direction = direction;
        }

        public static SortItem Create(Expression expression, SortDirection? direction)
        {
            return new (expression, direction ?? SortDirection.Undefined);
        }

        public SortItem Ascending() => new (_expression, SortDirection.Ascending);
        public SortItem Descending() => new (_expression, SortDirection.Descending);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Expressions.NameOrExpression(_expression).Visit(cypherVisitor);
            if (_direction != SortDirection.Undefined)
            {
                _direction?.Visit(cypherVisitor);
            }
            cypherVisitor.Leave(this);
        }
    }
}

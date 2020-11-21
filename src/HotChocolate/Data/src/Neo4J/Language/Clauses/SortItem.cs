using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public class SortItem : Visitable
    {
        private readonly Expression _expression;
        private readonly SortDirection _direction;

        public SortItem(Expression expression, SortDirection direction)
        {
            _expression = expression;
            _direction = direction;
        }

        public static SortItem Create(Expression expression, SortDirection direction)
        {
            return new SortItem(expression, direction ?? SortDirection.Undefined);
        }

        public SortItem Ascending() => new SortItem(_expression, SortDirection.Asc);
        public SortItem Descending() => new SortItem(_expression, SortDirection.Desc);

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            Expressions.NameOrExpression(_expression).Visit(visitor);
            if (_direction != SortDirection.Undefined)
            {
                _direction.Visit(visitor);
            }
            visitor.Leave(this);
        }
    }
}
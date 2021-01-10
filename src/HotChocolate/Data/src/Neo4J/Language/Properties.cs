using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents properties of a node or a relationship.
    /// </summary>
    public class Properties : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Properties;
        private readonly MapExpression _properties;

        public Properties(MapExpression properties)
        {
            _properties = properties;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _properties.Visit(visitor);
            visitor.Leave(this);
        }
    }
}

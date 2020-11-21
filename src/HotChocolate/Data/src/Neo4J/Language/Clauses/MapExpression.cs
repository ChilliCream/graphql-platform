using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class MapExpression : TypedSubtree<Expression, MapExpression>
    {
        private MapExpression(List<Expression> children) : base(children) { }

        public static MapExpression Create(object[] input)
        {
            List<Expression> newContent = new List<Expression>();
            HashSet<string> knownKeys = new HashSet<string>();

        }
    }
}
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class MapExpression : TypedSubtree<Expression, MapExpression>
    {
        private MapExpression(List<Expression> children) : base(children) { }

        public static MapExpression Create()
        {
            var newContent = new List<Expression>();
            //var knownKeys = new HashSet<string>();

            return new MapExpression(newContent);
        }
    }
}

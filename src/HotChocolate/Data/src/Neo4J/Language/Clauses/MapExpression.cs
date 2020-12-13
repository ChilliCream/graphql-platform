using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class MapExpression : TypedSubtree<Expression, MapExpression>
    {
        public override ClauseKind Kind => ClauseKind.Default;
        private MapExpression(List<Expression> children) : base(children) { }

        public static MapExpression Create(object[] input)
        {
            var newContent = new List<Expression>();
            var knownKeys = new HashSet<string>();

            for (var i = 0; i < input.Length; i += 2)
            {
                var entry = new KeyValueMapEntry((string)input[i], (Expression)input[i + 1]);
                newContent.Add(entry);
                knownKeys.Add(entry.GetKey());
            }

            return new MapExpression(newContent);
        }

        public static MapExpression WithEntries(List<Expression> entries)
        {
            return new MapExpression(entries);
        }

        public MapExpression AddEntries(List<Expression> entries)
        {
            var newContent = new List<Expression>();
            newContent.AddRange(this.GetChildren());
            newContent.AddRange(entries);

            return new MapExpression(newContent);
        }
    }
}

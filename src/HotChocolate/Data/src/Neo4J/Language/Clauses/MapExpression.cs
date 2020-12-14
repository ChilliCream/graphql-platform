using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class MapExpression : Visitable// : TypedSubtree<Expression, MapExpression>
    {
        public override ClauseKind Kind => ClauseKind.MapExpression;

        private readonly List<KeyValueMapEntry> _expressions;

        //private MapExpression(List<Expression> children) : base(children) { }

        public MapExpression(List<KeyValueMapEntry> expressions)
        {
            _expressions = expressions;
        }

        public static MapExpression Create(object[] input)
        {
            var newContent = new List<KeyValueMapEntry>();
            var knownKeys = new HashSet<string>();

            for (var i = 0; i < input.Length; i += 2)
            {
                var entry = new KeyValueMapEntry((string)input[i], (ILiteral)input[i + 1]);
                newContent.Add(entry);
                knownKeys.Add(entry.Key);
            }
            return new MapExpression(newContent);

            //return new MapExpression(newContent);
        }

        public List<KeyValueMapEntry> GetValues() => _expressions;

        // public static MapExpression WithEntries(List<Expression> entries)
        // {
        //     return new MapExpression(entries);
        // }

        // public MapExpression AddEntries(List<Expression> entries)
        // {
        //     var newContent = new List<Expression>();
        //     newContent.AddRange(this.GetChildren());
        //     newContent.AddRange(entries);
        //
        //     return new MapExpression(newContent);
        // }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            //_expressions?.ForEach(e => e.Visit(visitor));
            visitor.Leave(this);
        }
    }
}

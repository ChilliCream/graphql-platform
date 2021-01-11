using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// List of expressions on a node or relationship.
    /// </summary>
    public class MapExpression : Expression
    {
        public override ClauseKind Kind => ClauseKind.MapExpression;
        private readonly List<Expression> _expressions;

        private MapExpression(List<Expression> expressions)
        {
            _expressions = expressions;
        }

        public static MapExpression Create(params object[] input)
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
             return new(entries);
        }

        public MapExpression AddEntries(IEnumerable<Expression> entries)
        {
            var newContent = new List<Expression>();
            newContent.AddRange(_expressions);
            newContent.AddRange(entries);

            return new MapExpression(newContent);
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _expressions.ForEach(e =>
            {
                if (_expressions.Count > 1)
                {
                    e.Visit(visitor);
                    KeyValueSeparator.Instance.Visit(visitor);
                }
                else
                    e.Visit(visitor);

            });
            visitor.Leave(this);
        }
    }
}

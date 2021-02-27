using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A dedicated map expression.
    /// </summary>
    public class MapExpression : TypedSubtree<Expression>, IExpression
    {
        public override ClauseKind Kind => ClauseKind.MapExpression;

        private MapExpression(List<Expression> expressions) : base(expressions) { }

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

        public static MapExpression WithEntries(List<Expression> entries) =>
            new(entries);

        public MapExpression AddEntries(IEnumerable<Expression> entries)
        {
            var newContent = new List<Expression>();
            newContent.AddRange(Children);
            newContent.AddRange(entries);

            return new MapExpression(newContent);
        }

        protected override IVisitable PrepareVisit(Expression child) =>
            Expressions.NameOrExpression(child);

        /*public override void Visit(CypherVisitor visitor)
        {
            var singleExpression = _children.Any() && !_children.Skip(1).Any();
            var hasManyExpressions = _children.Any() && _children.Skip(1).Any();

            visitor.Enter(this);
            if(singleExpression)
                _children.First().Visit(visitor);
            else if (hasManyExpressions)
            {
                foreach (Expression expression in _children)
                {
                    if (_children.IndexOf(expression) == _children.Count - 1)
                    {
                        expression.Visit(visitor);
                        break;
                    }
                    expression.Visit(visitor);
                    KeyValueSeparator.Instance.Visit(visitor);
                }
            }
            visitor.Leave(this);
        }*/
    }
}

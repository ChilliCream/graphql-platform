using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A dedicated map expression.
    /// </summary>
    public class MapExpression
        : TypedSubtree<Expression>
        , ITypedSubtree
    {
        private MapExpression(List<Expression> expressions) : base(expressions)
        {
        }

        public override ClauseKind Kind => ClauseKind.MapExpression;

        public MapExpression AddEntries(IEnumerable<Expression> entries)
        {
            var newContent = new List<Expression>();
            newContent.AddRange(Children);
            newContent.AddRange(entries);

            return new MapExpression(newContent);
        }

        protected override IVisitable PrepareVisit(Expression child) =>
            Expressions.NameOrExpression(child);

        public static MapExpression WithEntries(List<Expression> entries) =>
            new(entries);

        public static MapExpression Create(params object[] input)
        {
            Ensure.IsTrue(input.Length % 2 == 0, "Need an even number of input parameters");
            var newContent = new List<Expression>();
            var knownKeys = new HashSet<string>();

            for (var i = 0; i < input.Length; i += 2)
            {
                var entry = new KeyValueMapEntry((string)input[i], (Expression)input[i + 1]);
                newContent.Add(entry);
                knownKeys.Add(entry.Key);
            }

            return new MapExpression(newContent);
        }
    }
}

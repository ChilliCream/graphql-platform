using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class CypherBuilder
    {
        private readonly List<Visitable> _clauses = new();

        public CypherBuilder Match(Visitable patternElement)
        {
            var matchClause = new Match(false, patternElement, false);
            _clauses.Add(matchClause);
            return this;
        }

        public static CypherBuilder Builder() => new();

        public string Build()
        {
            var visitor = new CypherVisitor();
            _clauses.ForEach(c => c.Visit(visitor));
            return visitor.Print();
        }
    }
}

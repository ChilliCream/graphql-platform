using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class StatementBuilder
    {
        private readonly Match? _match;
        private readonly Where? _where;
        private readonly ReturnClause? _return;

        public string Build()
        {
            using var visitor = new CypherVisitor();
            _match.Visit(visitor);
            _where.Visit(visitor);
            _where.Visit(visitor);
            return visitor.Print();
        }
    }

    public class MatchBuilder
    {
        private readonly List<PatternElement> _patternList;
    }
}

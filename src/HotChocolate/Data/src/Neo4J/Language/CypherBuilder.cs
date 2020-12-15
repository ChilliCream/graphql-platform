using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class CypherBuilder
    {
        /// <summary>
        /// Current list of reading or update clauses to be generated.
        /// </summary>
        private readonly List<Visitable> _clauses = new();

        private readonly List<PatternElement> _patternElements;
        private readonly Dictionary<string, string> _filterDefinition;
        private readonly Dictionary<string, string> _sortDefinition;
        private readonly Dictionary<string, string> _paginatinDefinition;
        private readonly Dictionary<string, string> _projectionDefinition;

        public CypherBuilder Create(IPatternElement[] pattern)
        {
            //var clause = new Create(pattern);
            //_clauses.Add(matchClause);
            return this;
        }
        public static CypherBuilder Builder() => new();

        private enum UpdateType {
            Delete,
            DETACH_DELETE,
            SET,
            REMOVE,
            CREATE,
            MERGE
        }
        public string Build()
        {
            var visitor = new CypherVisitor();
            _clauses.ForEach(c => c.Visit(visitor));
            return visitor.Print();
        }
    }
}

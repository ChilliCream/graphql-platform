using System.Collections.Generic;
using HotChocolate.Data.Neo4j.Language.Clauses;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4j
{
    public partial class Cypher
    {

        private readonly CypherVisitor _visitor = new CypherVisitor();
        private List<IVisitable> _clauses = new List<IVisitable>();

        public Cypher Match(bool optional)
        {
            var clause = new MatchClause();
            _clauses.Add(clause);

            return this;
        }

        public Cypher Match(NodeClause node)
        {
            var clause = new MatchClause(true, node);
            _clauses.Add(clause);
            return this;
        }

        public static NodeClause Node(string alias, string label)
        {
            var clause = new NodeClause(alias, label);
            return clause;
        }


        public string Print()
        {
            _clauses.ForEach(c => c.Visit(_visitor));
           return _visitor.ToString();
        }
    }
}

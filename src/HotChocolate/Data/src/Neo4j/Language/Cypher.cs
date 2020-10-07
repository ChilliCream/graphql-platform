using System.Collections.Generic;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4j
{
    public partial class Cypher
    {
        private IDriver _connection;
        private readonly CypherVisitor _visitor = new CypherVisitor();
        private List<IVisitable> _clauses = new List<IVisitable>();

        public Cypher Connect(Neo4jClient client)
        {
            _connection = client.Connection;
            return this;
        }

        public Node Node(string alias, List<string> labels)
        {
            return new Node(alias, labels);
        }

        public Cypher Match(bool optional)
        {
            var clause = new Match();
            _clauses.Add(clause);

            return this;
        }

        public Cypher Match(Node node)
        {
            var clause = new Match(true, node);
            _clauses.Add(clause);
            return this;
        }


        public string Print()
        {
            _clauses.ForEach(c => c.Visit(_visitor));
           return _visitor.ToString();
        }

    }
}

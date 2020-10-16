using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4j.Language;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4j
{
    public class Cypher
    {
        private IAsyncSession? _asyncSession;
        private bool _isRead = true;
        private readonly CypherVisitor _cypherVisitor = new CypherVisitor();
        private readonly CypherContext _cypherContext = new CypherContext();
        private List<IVisitable> _clauses = new List<IVisitable>();

        public Cypher() { }

        public Cypher(IAsyncSession asyncSession)
        {
            _asyncSession = asyncSession;
        }

        public Node Node(string alias, List<string> labels)
        {
            return new Node(alias, labels);
        }

        public Cypher Match(Node node, bool optional = false)
        {
            var clause = new Match(node, optional);
            _clauses.Add(clause);

            return this;
        }

        public Cypher Return(Node node)
        {
            _clauses.Add(new Return(node));
            return this;
        }

        public async Task<IResultCursor> ExecuteAsync()
        {
            if (_asyncSession == default)
            {
                throw new ArgumentException(nameof(_asyncSession));
            }
            return await _asyncSession.RunAsync(new Query(Print())).ConfigureAwait(false);
        }

        public string Print()
        {
            _clauses.ForEach(c => c.Visit(_cypherVisitor));
           return _cypherVisitor.ToString();
        }
    }
}

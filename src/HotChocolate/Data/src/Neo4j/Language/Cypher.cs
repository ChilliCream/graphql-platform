using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jMapper;
using System.Threading;

namespace HotChocolate.Data.Neo4j
{
    public class Cypher<T> : IExecutable<T>, IDisposable
    {
        private readonly IAsyncSession _asyncSession;
        private readonly CypherVisitor _cypherVisitor = new CypherVisitor();
        private readonly List<IVisitable> _clauses = new List<IVisitable>();

        public Cypher(IAsyncSession asyncSession)
        {
            _asyncSession = asyncSession;
        }

        public Node Node(string alias, List<string> labels)
        {
            return new Node(alias, labels);
        }

        public Cypher<T> Match(Node node, bool optional = false)
        {
            var clause = new Match(node, optional);
            _clauses.Add(clause);

            return this;
        }

        public Cypher<T> Return(Node node)
        {
            _clauses.Add(new Return(node));
            return this;
        }

        public string Print()
        {
            _clauses.ForEach(c => c.Visit(_cypherVisitor));
           return _cypherVisitor.ToString();
        }

        async ValueTask<object> IExecutable.ExecuteAsync(CancellationToken cancellationToken)
        {
            return await ExecuteAsync(cancellationToken);
        }

        public async ValueTask<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken)
        {
            IResultCursor cursor = await _asyncSession.RunAsync(new Query(Print())).ConfigureAwait(false);
            return await cursor.MapAsync<T>().ConfigureAwait(false);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

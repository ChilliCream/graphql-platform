using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor : IDisposable
    {
        private readonly CypherWriter _writer = new CypherWriter();

        //public CypherQuery Query { get; } = new CypherQuery();
        public string Print() => _writer.Print();

        public void Dispose()
        {

        }
    }
}

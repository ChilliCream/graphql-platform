using System;

namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor : IDisposable
    {
        private readonly CypherBuilder _builder = new CypherBuilder();

        //public CypherQuery Query { get; } = new CypherQuery();
        public string Print() => _builder.Print();

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

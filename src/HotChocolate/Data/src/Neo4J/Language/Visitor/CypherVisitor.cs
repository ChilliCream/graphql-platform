using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor : IDisposable
    {
        private readonly CypherWriter _writer = new ();
        private readonly LinkedList<IVisitable> _visitedElements = new();

        //public CypherQuery Query { get; } = new CypherQuery();
        public string Print() => _writer.Print();

        public void Dispose()
        {

        }
    }
}

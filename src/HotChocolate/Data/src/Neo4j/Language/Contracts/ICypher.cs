using System.Collections.Generic;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4j
{
    public interface ICypher
    {
        public Cypher Match(string aliasParameter, List<string>? labels, object properties);
        public Cypher Returning(List<string> values);
        public CypherQuery Build();
        public string Print();
        public IResultCursor ExecuteAsync();
    }
}

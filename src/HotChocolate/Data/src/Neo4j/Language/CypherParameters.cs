using System.Collections.Generic;

namespace HotChocolate.Data.Neo4j
{
    public class CypherParameters : Dictionary<string, object>
    {
        public CypherParameters() { }

        public CypherParameters(IDictionary<string, object?> dictionary) : base(dictionary)
        {
        }
    }
}

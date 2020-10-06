using System.Collections.Generic;

namespace HotChocolate.Data.Neo4j
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "<Pending>")]
    public class CypherParameters : Dictionary<string, object>
    {
        public CypherParameters() { }

        public CypherParameters(IDictionary<string, object?> dictionary) : base(dictionary)
        {
        }
    }
}

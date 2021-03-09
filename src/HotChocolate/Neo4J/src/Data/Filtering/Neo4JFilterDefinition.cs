using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JFilterDefinition : Condition
    {
        public override ClauseKind Kind { get; }
    }
}

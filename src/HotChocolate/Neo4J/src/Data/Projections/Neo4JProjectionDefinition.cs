using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Projections
{
    public class Neo4JProjectionDefinition
    {
        public Expression[] Expressions { get; }
    }
}

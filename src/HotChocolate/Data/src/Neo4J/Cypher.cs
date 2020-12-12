using System;
using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// The main entry point into the Cypher DSL.
    /// The Cypher Builder API is intended for framework usage to produce Cypher statements required for database operations.
    /// </summary>
    public static class Cypher
    {
        public static Node Node(string primaryLabel, string[] additionalLabels)
        {
            return Language.Node.Create(primaryLabel, additionalLabels);
        }
    }
}

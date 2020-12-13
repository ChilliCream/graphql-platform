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

        // Nodes
        // FilterDefinition
        // SortDefinition
        // ProjectionDefinition
        // PaginationDefinition

        //private List<Visitable> _clauses = new List<Visitable>();

        public static Node Node(string primaryLabel)
        {
            return Language.Node.Create(primaryLabel);
        }

        public static Node Node(string primaryLabel, string[] additionalLabels)
        {
            return Language.Node.Create(primaryLabel, additionalLabels);
        }

        public static string Build(Visitable statement)
        {
            using var visitor = new CypherVisitor();
            // _clauses.ForEach(c => c.Visit(visitor));

            return visitor.Print();

        }
    }
}

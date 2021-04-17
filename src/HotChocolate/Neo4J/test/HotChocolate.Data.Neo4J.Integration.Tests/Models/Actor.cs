using System.Collections.Generic;
using HotChocolate.Data.Neo4J;

namespace HotChocolate.Data.Integration.Tests
{

    [Neo4JNode("Actor")]
    public class Actor
    {
        public string Name { get; set; }

        [Neo4JRelationship("ACTED_IN", RelationshipDirection.Incoming)]
        public List<Movie> ActedIn { get; set; }
    }
}

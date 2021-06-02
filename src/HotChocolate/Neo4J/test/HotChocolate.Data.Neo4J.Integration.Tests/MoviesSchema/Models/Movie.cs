using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Execution;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Integration
{
    [Neo4JNode("Movie")]
    public class Movie
    {
        public string Title { get; set; }

        [Neo4JRelationship("ACTED_IN")]
        public List<Actor> Generes { get; set; }
    }
}

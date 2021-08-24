using System.Collections.Generic;
using HotChocolate.Data.Neo4J;

namespace HotChocolate.Data.Neo4J.Integration.AnnotationBased
{
    [Neo4JNode("Actor")]
    public class Actor
    {
        public string Name { get; set; }

        [Neo4JRelationship("ACTED_IN")]
        public List<Movie> ActedIn { get; set; }
    }
}

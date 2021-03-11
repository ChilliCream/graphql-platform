using System.Collections.Generic;
using HotChocolate.Data.Neo4J;

namespace Neo4jDemo.Models
{
    [Neo4JNode("User")]
    public class User
    {
        [Neo4JNodeId]
        public long? Id { get; set; }
        public string Name { get; set; }

        [Neo4JRelationship("WROTE")]
        public List<Review> Reviews { get; set; }
    }
}

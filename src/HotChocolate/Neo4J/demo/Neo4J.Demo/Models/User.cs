using System.Collections.Generic;
using HotChocolate.Data.Neo4J;

namespace Neo4jDemo.Models
{
    public class User
    {
        public string Name { get; set; }

        [Neo4JRelationship("WROTE")]
        public List<Review> Reviews { get; set; }
    }
}

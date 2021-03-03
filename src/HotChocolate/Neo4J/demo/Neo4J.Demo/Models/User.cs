using System.Collections.Generic;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Language;

namespace Neo4jDemo.Models
{
    public class User
    {
        public string Name { get; set; }

        //[Neo4JRelationship("WROTE", RelationshipDirection.Outgoing)]
        public List<Review> Reviews { get; set; }
    }
}

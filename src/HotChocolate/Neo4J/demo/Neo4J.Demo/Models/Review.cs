using HotChocolate.Data;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Language;

namespace Neo4jDemo.Models
{
    public class Review
    {
        public double Rating { get; set; }
        public string Text { get; set; }

        [Neo4JRelationship("WROTE", RelationshipDirection.Incoming)]
        public User User { get; set; }

        [Neo4JRelationship("REVIEWS", RelationshipDirection.Outgoing)]
        public Business Business { get; set; }
    }
}

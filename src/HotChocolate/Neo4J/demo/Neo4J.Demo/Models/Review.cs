using HotChocolate.Data.Neo4J;

namespace Neo4jDemo.Models
{
    public class Review
    {
        public double Rating { get; set; }
        public string Text { get; set; }

        [Neo4JRelationship("WROTE")]
        public User User { get; set; }

        [Neo4JRelationship("REVIEWS")]
        public Business Business { get; set; }
    }
}

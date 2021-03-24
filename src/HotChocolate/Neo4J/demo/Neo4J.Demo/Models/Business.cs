using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Data.Neo4J;

namespace Neo4jDemo.Models
{
    [Neo4JNode("Business")]
    public class Business
    {
        [Neo4JNodeId, GraphQLIgnore]
        public long? Id { get; set; }
        [GraphQLNonNullType]
        public string Name { get; set; }
        [GraphQLNonNullType]
        public string City { get; set; }
        [GraphQLNonNullType]
        public string State { get; set; }

        [Neo4JRelationship("REVIEWS", RelationshipDirection.Incoming)]
        public List<Review> Reviews { get; set; }

        [Neo4JCypher(@"MATCH (this)<-[:REVIEWS]-(r:Review) RETURN avg(r.stars)")]
        public double? AverageRating { get; set; }
    }
}

using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Language;

namespace Neo4jDemo.Models
{
    public class Business
    {
        [GraphQLNonNullType]
        public string Name { get; set; }
        [GraphQLNonNullType]
        public string City { get; set; }
        [GraphQLNonNullType]
        public string State { get; set; }

        //[Neo4JRelationship("REVIEWS", RelationshipDirection.)]
        public List<Review> Reviews { get; set; }

        [Neo4JCypher(@"MATCH (this)<-[:REVIEWS]-(r:Review) RETURN avg(r.stars)")]
        public double AverageRating { get; set; }
    }
}

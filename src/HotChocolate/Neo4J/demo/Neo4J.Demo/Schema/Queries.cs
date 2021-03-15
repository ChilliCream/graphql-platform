using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Neo4J.Paging;
using HotChocolate.Types;
using Neo4j.Driver;
using Neo4jDemo.Models;

namespace Neo4jDemo.Schema
{
    [ExtendObjectType(Name = "Query")]
    public class Queries
    {
        // Would like to add this for someone that wants projections, filtering, sorting, pagination all together without boilerplate
        // [UseNeo4J(database="neo4j")]
        // public Neo4JExecutable<Business> Businesses() => new();

        [UseNeo4JDatabase("neo4j")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Neo4JExecutable<Business> Businesses([ScopedService] IAsyncSession session) =>
            new (session);

        [UseNeo4JDatabase("neo4j")]
        [UseProjection]
        //[UseFiltering]
        [UseSorting]
        public Neo4JExecutable<User> Users([ScopedService] IAsyncSession session) =>
            new (session);

        [UseNeo4JDatabase("neo4j")]
        [UseProjection]
        //[UseFiltering]
        [UseSorting]
        public Neo4JExecutable<Review> Reviews([ScopedService] IAsyncSession session) =>
            new (session);
    }
}

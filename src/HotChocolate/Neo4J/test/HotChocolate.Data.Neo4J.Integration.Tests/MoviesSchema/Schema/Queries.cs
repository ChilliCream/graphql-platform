using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Types;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Integration
{
    [ExtendObjectType(Name = "Query")]
    public class Queries
    {
        [UseNeo4JDatabase("neo4j")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Neo4JExecutable<Actor> Actors([ScopedService] IAsyncSession session) => new(session);

        [UseNeo4JDatabase("neo4j")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public Neo4JExecutable<Movie> Movies([ScopedService] IAsyncSession session) => new(session);
    }
}
